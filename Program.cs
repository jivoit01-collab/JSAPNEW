using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using JSAPNEW.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using JSAPNEW.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JSAPNEW.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<SessionAuthFilter>();
});

var jwtSecret = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException("JWT SecretKey must be configured in appsettings.json.");

var jwtSecretBytes = Encoding.UTF8.GetBytes(jwtSecret);
if (jwtSecretBytes.Length < 32)
    throw new InvalidOperationException("JWT secret must be at least 256 bits (32 bytes).");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "JSAP";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "JSAPClients";

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(builder.Configuration.GetValue<int>("Session:TimeoutDays"));
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = builder.Environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict;
    options.Cookie.MaxAge = TimeSpan.FromDays(7);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "JSAP API", Version = "v1" });
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
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = false;
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrWhiteSpace(jti))
                {
                    context.Fail("Token is missing jti.");
                    return;
                }

                var authSecurity = context.HttpContext.RequestServices.GetRequiredService<IAuthSecurityService>();
                if (await authSecurity.IsAccessTokenRevokedAsync(jti))
                    context.Fail("Token has been revoked.");
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"success\":false,\"message\":\"Authentication required\"}");
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"success\":false,\"message\":\"Access forbidden\"}");
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(jwtSecretBytes),
            ClockSkew = TimeSpan.Zero,
            RequireExpirationTime = true
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "Super User"));
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin", "Super User"));
});

// Rate limiting disabled for development - uncomment for production
/*
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("LoginPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"login:{context.Connection.RemoteIpAddress}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var partitionKey = !string.IsNullOrWhiteSpace(userId)
            ? $"user:{userId}"
            : $"ip:{context.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});
*/

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

var appUrl = builder.Configuration["App:Url"] ?? "http://localhost:5000";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins(appUrl, "http://localhost:3000", "http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthSecurityService, AuthSecurityService>();
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

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://code.jquery.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://kit.fontawesome.com https://cdn.sheetjs.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com https://fonts.gstatic.com; " +
        "img-src 'self' data: https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
        "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com; " +
        "frame-ancestors 'none'; object-src 'none'; base-uri 'self';";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["X-XSS-Protection"] = "0";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    if (!context.Request.IsHttps && !app.Environment.IsDevelopment())
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("HTTPS is required.");
        return;
    }
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowSpecificOrigin");
app.UseSession();
// app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");
app.MapControllers();

app.MapGet("/health", [AllowAnonymous] () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    app = "JSAP"
}));

app.Run();
