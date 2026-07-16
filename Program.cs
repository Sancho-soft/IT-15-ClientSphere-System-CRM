using ClientSphere.Data;
using ClientSphere.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));





builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = true;
    // Enforce strict password requirements for security
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders()
    .AddPasswordValidator<ClientSphere.Services.PwnedPasswordValidator<ApplicationUser>>();

// Set password reset token lifespan to 24 hours
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(24);
});

builder.Services.AddHttpClient<ClientSphere.Services.PwnedPasswordValidator<ApplicationUser>>();

builder.Services.AddScoped<ClientSphere.Repositories.ICustomerRepository, ClientSphere.Repositories.CustomerRepository>();
builder.Services.AddScoped<ClientSphere.Repositories.IProductRepository, ClientSphere.Repositories.ProductRepository>();
builder.Services.AddScoped<ClientSphere.Repositories.IOrderRepository, ClientSphere.Repositories.OrderRepository>();

builder.Services.AddScoped<ClientSphere.Services.ICustomerService, ClientSphere.Services.CustomerService>();
builder.Services.AddScoped<ClientSphere.Services.IProductService, ClientSphere.Services.ProductService>();
builder.Services.AddScoped<ClientSphere.Services.IOrderService, ClientSphere.Services.OrderService>();

builder.Services.AddScoped<ClientSphere.Services.ILeadService, ClientSphere.Services.LeadService>();
builder.Services.AddScoped<ClientSphere.Services.IOpportunityService, ClientSphere.Services.OpportunityService>();
builder.Services.AddScoped<ClientSphere.Services.ISupportService, ClientSphere.Services.SupportService>();
builder.Services.AddScoped<ClientSphere.Services.ICampaignService, ClientSphere.Services.CampaignService>();
builder.Services.AddScoped<ClientSphere.Services.IInvoiceService, ClientSphere.Services.InvoiceService>();

// API Integration Services
builder.Services.AddHttpClient<ClientSphere.Services.IIpGeolocationService, ClientSphere.Services.IpGeolocationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
});
builder.Services.AddHttpClient<ClientSphere.Services.ITurnstileService, ClientSphere.Services.TurnstileService>();
builder.Services.AddScoped<ClientSphere.Services.IPaymongoService, ClientSphere.Services.PaymongoService>();
builder.Services.AddScoped<ClientSphere.Services.ICloudinaryService, ClientSphere.Services.CloudinaryService>();
builder.Services.AddScoped<ClientSphere.Services.IEmailService, ClientSphere.Services.GmailSmtpEmailService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, ClientSphere.Services.GmailSmtpEmailService>();
builder.Services.AddScoped<ClientSphere.Services.ICalendarService, ClientSphere.Services.GraphCalendarService>();
builder.Services.AddScoped<ClientSphere.Services.ISystemSettingService, ClientSphere.Services.SystemSettingService>();
builder.Services.AddScoped<ClientSphere.Services.INotificationService, ClientSphere.Services.NotificationService>();
builder.Services.AddSingleton<ClientSphere.Services.RateLimitCacheService>(); // Dynamic Rate Limiting Cache

builder.Services.AddScoped<ClientSphere.Filters.AuditLogFilter>();
builder.Services.AddScoped<ClientSphere.Filters.EnforceMfaFilter>();
builder.Services.AddScoped<ClientSphere.Filters.ReadOnlySuperAdminFilter>();
builder.Services.AddControllersWithViews(options => {
    options.Filters.Add<ClientSphere.Filters.AuditLogFilter>();
    options.Filters.Add<ClientSphere.Filters.EnforceMfaFilter>();
    options.Filters.Add<ClientSphere.Filters.ReadOnlySuperAdminFilter>();
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var rateLimitCache = httpContext.RequestServices.GetRequiredService<ClientSphere.Services.RateLimitCacheService>();
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = rateLimitCache.CurrentApiRateLimit, // Dynamic Limit
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1) // per 1 minute per IP
            });
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

var defaultCulture = new System.Globalization.CultureInfo("en-PH");
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture),
    SupportedCultures = new[] { defaultCulture },
    SupportedUICultures = new[] { defaultCulture }
};
app.UseRequestLocalization(localizationOptions);

// Apply Migrations and Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(); // Apply any pending EF migrations automatically
        await DbInitializer.Initialize(services);

        // Initialize Rate Limit Cache
        var systemSettingsService = services.GetRequiredService<ClientSphere.Services.ISystemSettingService>();
        var rateLimitCache = services.GetRequiredService<ClientSphere.Services.RateLimitCacheService>();
        rateLimitCache.CurrentApiRateLimit = await systemSettingsService.GetSettingIntAsync("ApiRateLimit", 100);

        // Apply Session Timeout (Requirement 1)
        var sessionOptions = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<SessionOptions>>().Value;
        sessionOptions.IdleTimeout = TimeSpan.FromMinutes(await systemSettingsService.GetSettingIntAsync("SessionTimeoutMinutes", 30));

        // Apply Minimum Password Length (Requirement 2)
        var identityOptions = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Identity.IdentityOptions>>().Value;
        identityOptions.Password.RequiredLength = await systemSettingsService.GetSettingIntAsync("MinimumPasswordLength", 8);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage(); // FORCE DETAILED ERRORS FOR DEBUGGING
}

// Security Headers Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline' https://challenges.cloudflare.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com https://cdnjs.cloudflare.com; font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; img-src 'self' data: https://res.cloudinary.com; frame-src https://challenges.cloudflare.com; connect-src 'self'");
    context.Response.Headers.Append("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
    
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }
    
    await next();
});

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

// Audit logging for authorization failures
app.UseStatusCodePages(async statusCodeContext =>
{
    if (statusCodeContext.HttpContext.Response.StatusCode == 401 || statusCodeContext.HttpContext.Response.StatusCode == 403)
    {
        var logger = statusCodeContext.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        var path = statusCodeContext.HttpContext.Request.Path;
        var user = statusCodeContext.HttpContext.User.Identity?.Name ?? "Anonymous";
        logger.LogWarning("Authorization failure: User '{User}' was denied access to '{Path}' with status {StatusCode}.", user, path, statusCodeContext.HttpContext.Response.StatusCode);
    }
});
app.UseRateLimiter(); // Apply Rate Limiting

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();

app.UseAuthentication();

// Session IP Binding Middleware (Anti-Session-Hijacking)
app.Use(async (context, next) =>
{
    if (context.Session.IsAvailable && context.User.Identity?.IsAuthenticated == true)
    {
        var currentIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var sessionIp = context.Session.GetString("SessionIp");

        if (string.IsNullOrEmpty(sessionIp))
        {
            context.Session.SetString("SessionIp", currentIp);
        }
        else if (sessionIp != currentIp)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Session hijacking warning: session bound to IP '{SessionIp}' but request received from IP '{CurrentIp}'. Revoking authentication session.", sessionIp, currentIp);

            // Log out the user and clear session
            context.Session.Clear();
            var signInManager = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
            await signInManager.SignOutAsync();

            context.Response.Redirect("/Identity/Account/Login");
            return;
        }
    }
    await next();
});

app.UseAuthorization();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

await app.RunAsync();
