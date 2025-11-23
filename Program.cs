using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Ideku.Data.Context;
using Ideku.Data.Repositories;
using Ideku.Data.Repositories.WorkflowManagement;
using Ideku.Services.Auth;
using Ideku.Services.Email;
using Ideku.Services.Notification;
using Ideku.Services.Idea;
using Ideku.Services.Workflow;
using Ideku.Services.WorkflowManagement;
using Ideku.Services.Approver;
using Ideku.Services.IdeaRelation;
using Ideku.Services.ApprovalToken;
using Ideku.Models;
using Ideku.Models.Entities;
using WebOptimizer;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Configure globalization to use en-US culture (USD currency format)
var cultureInfo = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add WebOptimizer for bundling and minification
builder.Services.AddWebOptimizer(pipeline =>
{
    // Bundle and minify CSS files
    pipeline.AddCssBundle("/bundles/app.css", 
        "css/site.css", 
        "css/pages/*.css", 
        "css/components/*.css")
        .UseContentRoot();
    
    // Bundle and minify JavaScript files
    pipeline.AddJavaScriptBundle("/bundles/app.js", 
        "js/site.js", 
        "js/pages/*.js", 
        "js/common/*.js")
        .UseContentRoot();
});

// Add session and TempData support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddMemoryCache();

// Add Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "IdekuAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Add HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

// Configure Email Settings
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Configure File Upload Settings
builder.Services.Configure<Ideku.Configuration.FileUploadSettings>(
    builder.Configuration.GetSection("FileUploadSettings"));

// Register Services
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<INotificationService, NotificationService>();
builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddScoped<IApprovalTokenService, ApprovalTokenService>();
builder.Services.AddScoped<Ideku.Services.FileUpload.IFileUploadService, Ideku.Services.FileUpload.FileUploadService>();
builder.Services.AddScoped<IIdeaService, IdeaService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IApproverService, ApproverService>();
builder.Services.AddScoped<IWorkflowManagementService, WorkflowManagementService>();
builder.Services.AddScoped<IIdeaRelationService, IdeaRelationService>();
builder.Services.AddScoped<Ideku.Services.Lookup.ILookupService, Ideku.Services.Lookup.LookupService>();
builder.Services.AddScoped<Ideku.Services.Roles.IRolesService, Ideku.Services.Roles.RolesService>();
builder.Services.AddScoped<Ideku.Services.UserManagement.IUserManagementService, Ideku.Services.UserManagement.UserManagementService>();
builder.Services.AddScoped<Ideku.Services.IdeaImplementators.IIdeaImplementatorService, Ideku.Services.IdeaImplementators.IdeaImplementatorService>();
builder.Services.AddScoped<Ideku.Services.Milestone.IMilestoneService, Ideku.Services.Milestone.MilestoneService>();
builder.Services.AddScoped<Ideku.Services.ChangeWorkflow.IChangeWorkflowService, Ideku.Services.ChangeWorkflow.ChangeWorkflowService>();
builder.Services.AddScoped<Ideku.Services.BypassStage.IBypassStageService, Ideku.Services.BypassStage.BypassStageService>();
builder.Services.AddScoped<Ideku.Services.IdeaMonitoring.IIdeaMonitoringService, Ideku.Services.IdeaMonitoring.IdeaMonitoringService>();
builder.Services.AddScoped<Ideku.Services.AccessControl.IAccessControlService, Ideku.Services.AccessControl.AccessControlService>();
builder.Services.AddScoped<Ideku.Services.FileAttachment.IFileAttachmentService, Ideku.Services.FileAttachment.FileAttachmentService>();

// Register Background Services
builder.Services.AddHostedService<Ideku.Services.BackgroundServices.ActingRoleReversionService>();
builder.Services.AddHostedService<Ideku.Services.BackgroundServices.IdeaInactiveMonitorService>();

// Register Repositories
builder.Services.AddScoped<IIdeaRepository, IdeaRepository>();
builder.Services.AddScoped<ILookupRepository, LookupRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWorkflowRepository, WorkflowRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IApproverRepository, ApproverRepository>();
builder.Services.AddScoped<IWorkflowManagementRepository, WorkflowManagementRepository>();
builder.Services.AddScoped<Ideku.Data.Repositories.IRolesRepository, Ideku.Data.Repositories.RolesRepository>();
builder.Services.AddScoped<Ideku.Data.Repositories.IdeaImplementators.IIdeaImplementatorRepository, Ideku.Data.Repositories.IdeaImplementators.IdeaImplementatorRepository>();
builder.Services.AddScoped<IMilestoneRepository, MilestoneRepository>();
builder.Services.AddScoped<IIdeaMonitoringRepository, IdeaMonitoringRepository>();
builder.Services.AddScoped<Ideku.Data.Repositories.AccessControl.IAccessControlRepository, Ideku.Data.Repositories.AccessControl.AccessControlRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseWebOptimizer();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        SeedDatabase(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

app.Run();

// Database seeding method
static void SeedDatabase(IServiceProvider services)
{
    using var context = services.GetRequiredService<AppDbContext>();

    // Ensure database is created
    context.Database.EnsureCreated();

    // Seed all data in order (respecting foreign key dependencies)
    Ideku.Data.Seeders.RoleSeeder.Seed(context);
    Ideku.Data.Seeders.DivisionSeeder.Seed(context);
    Ideku.Data.Seeders.DepartmentSeeder.Seed(context);
    Ideku.Data.Seeders.CategorySeeder.Seed(context);
    Ideku.Data.Seeders.EventSeeder.Seed(context);
    Ideku.Data.Seeders.EmployeeSeeder.Seed(context);
    Ideku.Data.Seeders.UserSeeder.Seed(context);
    Ideku.Data.Seeders.AccessControlSeeder.Seed(context);
    Ideku.Data.Seeders.WorkflowSeeder.Seed(context);
}
