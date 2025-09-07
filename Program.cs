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
using Ideku.Models;
using Ideku.Models.Entities;
using WebOptimizer;

var builder = WebApplication.CreateBuilder(args);

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

// Register Services
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<INotificationService, NotificationService>();
builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddScoped<IIdeaService, IdeaService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IApproverService, ApproverService>();
builder.Services.AddScoped<IWorkflowManagementService, WorkflowManagementService>();
builder.Services.AddScoped<IIdeaRelationService, IdeaRelationService>();
builder.Services.AddScoped<Ideku.Services.Lookup.ILookupService, Ideku.Services.Lookup.LookupService>();

// Register Repositories
builder.Services.AddScoped<IIdeaRepository, IdeaRepository>();
builder.Services.AddScoped<ILookupRepository, LookupRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWorkflowRepository, WorkflowRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IApproverRepository, ApproverRepository>();
builder.Services.AddScoped<IWorkflowManagementRepository, WorkflowManagementRepository>();

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

    // Check if data already exists
    if (context.Roles.Any())
    {
        return; // DB has been seeded
    }

    // Seed Roles
    var roles = new[]
    {
        new Role { RoleName = "Superuser", Desc = "System Superuser" },
        new Role { RoleName = "Admin", Desc = "Manager with approval authority" },
        new Role { RoleName = "Initiator", Desc = "Idea Initiator" },
        new Role { RoleName = "Workstream Leader", Desc = "Workstream Leader" },
        new Role { RoleName = "Implementor", Desc = "Idea Implementor" },
        new Role { RoleName = "Mgr. Dept", Desc = "Department Manager" },
        new Role { RoleName = "GM Division", Desc = "General Manager Division" },
        new Role { RoleName = "GM Finance", Desc = "General Manager Finance" },
        new Role { RoleName = "GM BPID", Desc = "General Manager BPID" },
        new Role { RoleName = "COO", Desc = "Chief Operating Officer" },
        new Role { RoleName = "SCFO", Desc = "Senior Chief Financial Officer" },
        new Role { RoleName = "CEO", Desc = "Chief Executive Officer" },
        new Role { RoleName = "GM Division Act.", Desc = "GM Division Acting" },
        new Role { RoleName = "GM Finance Act.", Desc = "GM Finance Acting" },
        new Role { RoleName = "GM BPID Act.", Desc = "GM BPID Acting" },
        new Role { RoleName = "Mgr. Dept. Act.", Desc = "Department Manager Acting" },
        new Role { RoleName = "COO Act.", Desc = "COO Acting" },
        new Role { RoleName = "CEO Act.", Desc = "CEO Acting" },
        new Role { RoleName = "SCFO Act.", Desc = "SCFO Acting" }
    };
    context.Roles.AddRange(roles);
    context.SaveChanges();

    // Seed Divisions
    var divisions = new[]
    {
        new Division { Id = "D01", NameDivision = "Business & Performance Improvement", IsActive = true },
        new Division { Id = "D02", NameDivision = "Business Dev. & Risk Management", IsActive = true },
        new Division { Id = "D03", NameDivision = "Chief Executive Officer", IsActive = true },
        new Division { Id = "D04", NameDivision = "Chief Financial Officer", IsActive = true },
        new Division { Id = "D05", NameDivision = "Chief Operating Officer", IsActive = true },
        new Division { Id = "D06", NameDivision = "Coal Processing & Handling", IsActive = true },
        new Division { Id = "D07", NameDivision = "Contract Mining", IsActive = true },
        new Division { Id = "D08", NameDivision = "Director of Finance", IsActive = true },
        new Division { Id = "D09", NameDivision = "External Affairs & Sustainable Development", IsActive = true },
        new Division { Id = "D10", NameDivision = "Finance", IsActive = true }
    };
    context.Divisions.AddRange(divisions);
    context.SaveChanges();

    // Seed Departments
    var departments = new[]
    {
        new Department { Id = "P01", NameDepartment = "Business & Performance Improvement", DivisiId = "D01", IsActive = true },
        new Department { Id = "P02", NameDepartment = "Business Dev. & Risk Management", DivisiId = "D02", IsActive = true },
        new Department { Id = "P03", NameDepartment = "Chief Executive Officer", DivisiId = "D03", IsActive = true },
        new Department { Id = "P04", NameDepartment = "Business Analysis", DivisiId = "D04", IsActive = true },
        new Department { Id = "P05", NameDepartment = "Chief Financial Officer", DivisiId = "D04", IsActive = true },
        new Department { Id = "P06", NameDepartment = "Chief Operating Officer", DivisiId = "D05", IsActive = true },
        new Department { Id = "P07", NameDepartment = "CHT Operations", DivisiId = "D06", IsActive = true },
        new Department { Id = "P08", NameDepartment = "Coal Processing & Handling", DivisiId = "D06", IsActive = true },
        new Department { Id = "P09", NameDepartment = "Coal Technology", DivisiId = "D06", IsActive = true },
        new Department { Id = "P10", NameDepartment = "CPP Maintenance", DivisiId = "D06", IsActive = true },
        new Department { Id = "P11", NameDepartment = "CPP Operations", DivisiId = "D06", IsActive = true },
        new Department { Id = "P12", NameDepartment = "Infrastructure", DivisiId = "D06", IsActive = true },
        new Department { Id = "P13", NameDepartment = "Plant Engineering & Project Services", DivisiId = "D06", IsActive = true },
        new Department { Id = "P14", NameDepartment = "Power Generation & Transmission", DivisiId = "D06", IsActive = true },
        new Department { Id = "P15", NameDepartment = "CHT Maintenance", DivisiId = "D06", IsActive = true },
        new Department { Id = "P16", NameDepartment = "Contract Mining", DivisiId = "D07", IsActive = true },
        new Department { Id = "P17", NameDepartment = "Contract Mining Issues & Analysis", DivisiId = "D07", IsActive = true },
        new Department { Id = "P18", NameDepartment = "Mining Contract Bengalon", DivisiId = "D07", IsActive = true },
        new Department { Id = "P19", NameDepartment = "Mining Contract Pama", DivisiId = "D07", IsActive = true },
        new Department { Id = "P20", NameDepartment = "Mining Contract Sangatta", DivisiId = "D07", IsActive = true },
        new Department { Id = "P21", NameDepartment = "Mining Contract TCI Pits", DivisiId = "D07", IsActive = true },
        new Department { Id = "P22", NameDepartment = "Internal Audit", DivisiId = "D08", IsActive = true },
        new Department { Id = "P23", NameDepartment = "Bengalon Community Rels & Dev", DivisiId = "D09", IsActive = true },
        new Department { Id = "P24", NameDepartment = "Community Empowerment", DivisiId = "D09", IsActive = true },
        new Department { Id = "P25", NameDepartment = "Ext. Affairs & Sustainable Dev.", DivisiId = "D09", IsActive = true },
        new Department { Id = "P26", NameDepartment = "External Relations", DivisiId = "D09", IsActive = true },
        new Department { Id = "P27", NameDepartment = "Land Management", DivisiId = "D09", IsActive = true },
        new Department { Id = "P28", NameDepartment = "Project Management & Evaluation", DivisiId = "D09", IsActive = true },
        new Department { Id = "P29", NameDepartment = "Accounting and Reporting", DivisiId = "D10", IsActive = true },
        new Department { Id = "P30", NameDepartment = "Finance", DivisiId = "D10", IsActive = true },
        new Department { Id = "P31", NameDepartment = "Tax & Government Impost", DivisiId = "D10", IsActive = true },
        new Department { Id = "P32", NameDepartment = "Treasury", DivisiId = "D10", IsActive = true }
        
    };
    context.Departments.AddRange(departments);
    context.SaveChanges();

    // Seed Categories
    var categories = new[]
    {
        new Category { CategoryName = "General Transformation", IsActive = true },
        new Category { CategoryName = "Increase Revenue", IsActive = true },
        new Category { CategoryName = "Cost Reduction (CR)", IsActive = true },
        new Category { CategoryName = "Digitalization", IsActive = true },
    };
    context.Categories.AddRange(categories);
    context.SaveChanges();

    // Seed Events
    var Events = new[]
    {
        new Event { EventName = "Hackathon", IsActive = true },
        new Event { EventName = "CI Academy", IsActive = true },
    };
    context.Events.AddRange(Events);
    context.SaveChanges();

    // Seed Employees
    var employees = new[]
    {
        new Employee { EMP_ID = "EMP001", NAME = "Super User", POSITION_TITLE = "System Administrator", DIVISION = "D01", DEPARTEMENT = "P01", EMAIL = "some.other.email@example.com", POSITION_LVL = "Supervisor", EMP_STATUS = "Active" },
        new Employee { EMP_ID = "EMP002", NAME = "Faiq Lidan", POSITION_TITLE = "Frondend Developer", DIVISION = "D01", DEPARTEMENT = "P01", EMAIL = "faiqlidan03@gmail.com", POSITION_LVL = "Supervisor", EMP_STATUS = "Active" },
        new Employee { EMP_ID = "EMP003", NAME = "Mike Johnson", POSITION_TITLE = "Software Developer", DIVISION = "D05", DEPARTEMENT = "P06", EMAIL = "bpidstudent@gmail.com", POSITION_LVL = "Supervisor", EMP_STATUS = "Active" },
        new Employee { EMP_ID = "EMP004", NAME = "Sarah Wilson", POSITION_TITLE = "Finance Analyst", DIVISION = "D04", DEPARTEMENT = "P05", EMAIL = "sarah.wilson@company.com", POSITION_LVL = "Supervisor", EMP_STATUS = "Active" },
        new Employee { EMP_ID = "EMP005", NAME = "John Doe", POSITION_TITLE = "System Administrator", DIVISION = "D06", DEPARTEMENT = "P07", EMAIL = "admin@company.com", POSITION_LVL = "Supervisor", EMP_STATUS = "Active" }
    };
    context.Employees.AddRange(employees);
    context.SaveChanges();

    // Seed Users
    var users = new[]
    {
        new User { EmployeeId = "EMP001", RoleId = 1, Username = "superuser", Name = "Super User", IsActing = false },
        new User { EmployeeId = "EMP002", RoleId = 2, Username = "faiqlidan", Name = "Faiq Lidan", IsActing = false },
        new User { EmployeeId = "EMP003", RoleId = 4, Username = "workstream.leader", Name = "Mike Johnson (WSL)", IsActing = false }, // WORKSTREAM LEADER
        new User { EmployeeId = "EMP004", RoleId = 1, Username = "sarahwilson", Name = "Sarah Wilson", IsActing = false },
        new User { EmployeeId = "EMP005", RoleId = 3, Username = "johndoe", Name = "John Doe", IsActing = false }
    };
    context.Users.AddRange(users);
    context.SaveChanges();
}
