using ClientSphere.Data;
using ClientSphere.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));


builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    // TEMPORARY: Relax password requirements to allow email address as password
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 1;
    options.Password.RequiredUniqueChars = 0;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

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
builder.Services.AddScoped<ClientSphere.Services.IPaymentService, ClientSphere.Services.StripePaymentService>();
builder.Services.AddScoped<ClientSphere.Services.IEmailService, ClientSphere.Services.SendGridEmailService>();
builder.Services.AddScoped<ClientSphere.Services.ICalendarService, ClientSphere.Services.GraphCalendarService>();

builder.Services.AddScoped<ClientSphere.Filters.AuditLogFilter>();
builder.Services.AddControllersWithViews(options => {
    options.Filters.Add<ClientSphere.Filters.AuditLogFilter>();
});

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
// Configure the HTTP request pipeline.
// if (!app.Environment.IsDevelopment())
// {
//     app.UseExceptionHandler("/Home/Error");
//     // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//     app.UseHsts();
// }
app.UseDeveloperExceptionPage(); // FORCE DETAILED ERRORS FOR DEBUGGING

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
