using Microsoft.IdentityModel.Tokens;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using JSAPNEW.Services;

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
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");
app.MapControllers();

// Health check endpoint for CI/CD deployment verification
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    app = "JSAP"
})).AllowAnonymous();

app.Run();
