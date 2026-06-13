using Microsoft.IdentityModel.Tokens;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using JSAPNEW.Controllers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using JSAPNEW.Services;
using JSAPNEW.Security;
using JSAPNEW.Models;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
var authenticatedPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
    .RequireAuthenticatedUser()
    .Build();

builder.Services.AddControllers(options =>
{
    options.Filters.Add(new AuthorizeFilter(authenticatedPolicy));
});
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter(authenticatedPolicy));
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(builder.Configuration.GetValue<int>("Session:TimeoutDays"));
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.MaxAge = TimeSpan.FromDays(7);
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Your API", Version = "v1" });

    // Add JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };
            options.Events = new JwtBearerEvents
            {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrWhiteSpace(context.Token)
                    && string.IsNullOrWhiteSpace(context.Request.Headers.Authorization)
                    && context.Request.Cookies.TryGetValue(AuthCookieNames.AccessToken, out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var path = context.Request.Path.Value ?? string.Empty;
                var isApiRequest = path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/websession", StringComparison.OrdinalIgnoreCase);
                var acceptsHtml = !context.Request.Headers.Accept.Any()
                    || context.Request.Headers.Accept.Any(value =>
                        value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true
                        || value?.Contains("application/xhtml+xml", StringComparison.OrdinalIgnoreCase) == true
                        || value?.Contains("*/*", StringComparison.OrdinalIgnoreCase) == true);

                if (!context.Response.HasStarted && !isApiRequest && acceptsHtml)
                {
                    context.HandleResponse();
                    context.Response.Redirect("/Login");
                    return Task.CompletedTask;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Principal?.FindFirstValue("userId");
                var tokenStamp = context.Principal?.FindFirstValue("securityStamp");

                if (!int.TryParse(userIdValue, out var userId) || string.IsNullOrWhiteSpace(tokenStamp))
                {
                    context.Fail("Invalid token claims.");
                    return;
                }

                var connectionString = configuration.GetConnectionString("DefaultConnection");
                var secretKey = configuration["Jwt:SecretKey"];

                await using var connection = new SqlConnection(connectionString);
                var userRow = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT TOP 1 * FROM jsUser WHERE UserId = @UserId",
                    new { UserId = userId });

                if (userRow == null)
                {
                    context.Fail("User no longer exists.");
                    return;
                }

                var userValues = (IDictionary<string, object>)userRow;
                var roleRows = await connection.QueryAsync<dynamic>(
                    @"SELECT ur.userId, ur.roleId, r.roleName
                      FROM jsUserRole ur
                      LEFT JOIN jsRole r ON ur.roleId = r.roleId
                      WHERE ur.userId = @UserId
                      ORDER BY ur.roleId, r.roleName",
                    new { UserId = userId });
                var roleSnapshot = AuthSecurity.CreateRoleSnapshot(roleRows);
                var currentStamp = AuthSecurity.CreateSecurityStamp(userValues, secretKey, roleSnapshot);

                if (!string.Equals(tokenStamp, currentStamp, StringComparison.Ordinal))
                {
                    context.Fail("Token security stamp is no longer valid.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = authenticatedPolicy;
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin", "SuperAdmin", "Super User"));
});

// Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IBomService, BomService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddSingleton<SshService>();
builder.Services.AddScoped<IBom2Service, Bom2Service>();
builder.Services.AddScoped<IAdvanceRequestService, AdvanceRequestService>();
builder.Services.AddScoped<IReportsService, ReportsService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IItemMasterService, ItemMasterService>();
builder.Services.AddScoped<IBPMasterSapService, BPMasterSapService>();
builder.Services.AddScoped<IBPmasterService, BPmasterService>();
builder.Services.AddScoped<IDocumentDispatchService, DocumentDispatchService>();
builder.Services.AddScoped<IGIGOService, GIGOService>();
builder.Services.AddScoped<IInventoryAuditService, InventoryAuditService>();
builder.Services.AddScoped<ICreditLimitService, CreditLimitService>();
builder.Services.AddScoped<IPrdoService, PrdoService>();
builder.Services.AddScoped<IQcService, QcService>();
builder.Services.AddScoped<IAuth2Service, Auth2Service>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ITicketsService, TicketsService>();
builder.Services.AddScoped<IMakerService, MakerService>();
builder.Services.AddScoped<ICheckerService, CheckerService>();
builder.Services.AddScoped<IInvoicePaymentService, InvoicePaymentService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPaymentCheckerService, PaymentCheckerService>();
builder.Services.AddScoped<IHierarchyService, HierarchyService>();
builder.Services.AddScoped<IDocumentHubService, DocumentHubService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder
            .WithOrigins("http://localhost:5000") // Add your frontend URL
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

void ClearAuthState(HttpContext context)
{
    context.Session.Clear();
    var cookieOptions = new CookieOptions
    {
        HttpOnly = true,
        Secure = !app.Environment.IsDevelopment() || context.Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        IsEssential = true
    };

    context.Response.Cookies.Delete(AuthCookieNames.AccessToken, cookieOptions);
    context.Response.Cookies.Delete(AuthCookieNames.RefreshToken, cookieOptions);
}

void SetAuthCookies(HttpContext context, string accessToken, string refreshToken)
{
    var secure = !app.Environment.IsDevelopment() || context.Request.IsHttps;

    context.Response.Cookies.Append(AuthCookieNames.AccessToken, accessToken, new CookieOptions
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        IsEssential = true,
        Expires = DateTimeOffset.UtcNow.AddMinutes(15)
    });

    context.Response.Cookies.Append(AuthCookieNames.RefreshToken, refreshToken, new CookieOptions
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        IsEssential = true,
        Expires = DateTimeOffset.UtcNow.AddDays(7)
    });
}

ClaimsPrincipal BuildPrincipalFromToken(string accessToken)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var principal = tokenHandler.ValidateToken(accessToken, new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])),
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.Zero
    }, out _);

    return principal;
}

async Task<bool> TryRefreshWebAccessTokenAsync(HttpContext context, ILogger logger)
{
    if (!context.Request.Cookies.TryGetValue(AuthCookieNames.RefreshToken, out var refreshToken)
        || string.IsNullOrWhiteSpace(refreshToken))
    {
        return false;
    }

    try
    {
        var refreshTokenService = context.RequestServices.GetRequiredService<IRefreshTokenService>();
        var userService = context.RequestServices.GetRequiredService<IUserService>();
        var tokenService = context.RequestServices.GetRequiredService<ITokenService>();
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        var rotation = await refreshTokenService.RotateRefreshTokenAsync(refreshToken, ipAddress);
        if (!rotation.Success || string.IsNullOrWhiteSpace(rotation.RefreshToken))
        {
            if (rotation.ReplayDetected)
            {
                logger.LogWarning("Refresh token replay detected during web auto-refresh. UserId={UserId}", rotation.UserId);
            }

            ClearAuthState(context);
            return false;
        }

        var user = await userService.GetUserByIdAsync(rotation.UserId);
        if (user == null)
        {
            ClearAuthState(context);
            return false;
        }

        var accessToken = tokenService.GenerateToken(user);
        SetAuthCookies(context, accessToken, rotation.RefreshToken);
        context.User = BuildPrincipalFromToken(accessToken);
        return context.User.Identity?.IsAuthenticated == true;
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Web auto-refresh failed.");
        ClearAuthState(context);
        return false;
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Enable serving files from wwwroot
app.UseRouting();
app.UseCors("AllowSpecificOrigin");
app.UseSession();
app.UseAuthentication();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    var isAnonymousPath = path.Equals("/health", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/Login", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/Auth/login", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase);

    var isApiRequest = path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/websession", StringComparison.OrdinalIgnoreCase);
    var acceptsHtml = context.Request.Headers.Accept.Any(value =>
        value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true
        || value?.Contains("application/xhtml+xml", StringComparison.OrdinalIgnoreCase) == true
        || value?.Contains("*/*", StringComparison.OrdinalIgnoreCase) == true);
    var isHtmlLikeRequest = HttpMethods.IsGet(context.Request.Method)
        && !isApiRequest
        && (!context.Request.Headers.Accept.Any() || acceptsHtml);
    var isWebPageRequest = isHtmlLikeRequest;

    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
        .CreateLogger("AuthSessionMiddleware");

    if (!isApiRequest)
    {
        context.Response.Headers.CacheControl = "no-store, no-cache, max-age=0, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";
    }

    if (!isAnonymousPath && isWebPageRequest && context.User?.Identity?.IsAuthenticated != true)
    {
        if (await TryRefreshWebAccessTokenAsync(context, logger))
        {
            logger.LogDebug("Web access token auto-refreshed. Path={Path}", path);
        }
        else
        {
        logger.LogDebug("Redirecting unauthenticated web request to login. Path={Path}", path);
        context.Response.Redirect("/Login");
        return;
        }
    }

    if (context.User?.Identity?.IsAuthenticated == true)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("userId");

        if (!int.TryParse(userIdClaim, out var authenticatedUserId) || authenticatedUserId <= 0)
        {
            logger.LogWarning("Authenticated principal did not contain a valid userId claim. Path={Path}", path);
            ClearAuthState(context);
            if (isWebPageRequest)
            {
                context.Response.Redirect("/Login");
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!isWebPageRequest)
        {
            await next();
            return;
        }

        var sessionUserId = context.Session.GetInt32("userId");
        var companyList = context.Session.GetString("companyList");

        var needsSessionHydration = sessionUserId != authenticatedUserId || string.IsNullOrWhiteSpace(companyList);

        if (needsSessionHydration)
        {
            try
            {
                var userService = context.RequestServices.GetRequiredService<IUserService>();
                var companies = (await userService.GetCompanyAsync(authenticatedUserId))?.ToList() ?? new List<CompanyModel>();

                if (companies.Count == 0)
                {
                    logger.LogWarning("No companies found for authenticated user. UserId={UserId}, Path={Path}", authenticatedUserId, path);
                    ClearAuthState(context);
                    if (isWebPageRequest)
                    {
                        context.Response.Redirect("/Login");
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    }
                    return;
                }

                context.Session.SetInt32("userId", authenticatedUserId);
                context.Session.SetString(
                    "username",
                    context.User.FindFirstValue(ClaimTypes.Name)
                        ?? context.User.FindFirstValue("username")
                        ?? context.User.Identity?.Name
                        ?? "User");
                context.Session.SetString("companyList", JsonConvert.SerializeObject(companies));

                var selectedCompanyId = context.Session.GetInt32("selectedCompanyId");
                if (!selectedCompanyId.HasValue || !companies.Any(company => company.id == selectedCompanyId.Value))
                {
                    context.Session.SetInt32("selectedCompanyId", companies[0].id);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to hydrate session from claims. UserId={UserId}, Path={Path}", authenticatedUserId, path);
                ClearAuthState(context);
                if (isWebPageRequest)
                {
                    context.Response.Redirect("/Login");
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                }
                return;
            }
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=DashboardWeb}/{action=Index}/{id?}");
app.MapControllers();

// Health check endpoint for CI/CD deployment verification
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    app = "JSAP"
})).AllowAnonymous();

app.Run();
