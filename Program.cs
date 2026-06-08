using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Services;

var philippineCulture = new CultureInfo("en-PH");
CultureInfo.DefaultThreadCurrentCulture = philippineCulture;
CultureInfo.DefaultThreadCurrentUICulture = philippineCulture;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddScoped<BookingEngineService>();
builder.Services.AddScoped<BookingNotificationService>();
builder.Services.AddScoped<GuestPortalNotificationService>();
builder.Services.AddScoped<GuestPortalService>();
builder.Services.AddScoped<FinanceService>();
builder.Services.AddScoped<PaymentIntegrityService>();
builder.Services.AddScoped<ARCollectionReportService>();
builder.Services.AddScoped<AccountingPostingService>();
builder.Services.AddScoped<AccountingReportService>();
builder.Services.AddScoped<AccountsPayableService>();
builder.Services.AddScoped<CashFlowReportService>();
builder.Services.AddScoped<GroupManagementService>();
builder.Services.AddScoped<LaborCostingService>();
builder.Services.AddScoped<ExecutiveKPIService>();
builder.Services.AddScoped<DepartmentPerformanceService>();
builder.Services.AddScoped<ExecutiveAlertService>();
builder.Services.AddScoped<ExecutiveReportingService>();
builder.Services.AddScoped<OwnerReportPackageService>();
builder.Services.AddSingleton<ReportCatalogService>();
builder.Services.AddScoped<ReportExportService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<RevenueManagementService>();
builder.Services.AddScoped<AIPlaceholderService>();
builder.Services.AddScoped<ManagementDailySummaryService>();
builder.Services.AddScoped<ManagementInsightService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<SystemErrorLogService>();
builder.Services.AddScoped<SystemNotificationService>();
builder.Services.AddScoped<DataValidationService>();
builder.Services.AddScoped<DemoDataSeederService>();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PmsPolicies.AdminSetup, policy => policy.RequireRole(PmsRoles.AdminSetup));
    options.AddPolicy(PmsPolicies.FrontOffice, policy => policy.RequireRole(PmsRoles.FrontOffice));
    options.AddPolicy(PmsPolicies.Housekeeping, policy => policy.RequireRole(PmsRoles.Housekeeping));
    options.AddPolicy(PmsPolicies.Finance, policy => policy.RequireRole(PmsRoles.Finance));
    options.AddPolicy(PmsPolicies.FinanceApprovals, policy => policy.RequireRole(PmsRoles.FinanceApprovals));
    options.AddPolicy(PmsPolicies.AccountsReceivable, policy => policy.RequireRole(PmsRoles.AccountsReceivable));
    options.AddPolicy(PmsPolicies.FoodBeverageService, policy => policy.RequireRole(PmsRoles.FoodBeverageService));
    options.AddPolicy(PmsPolicies.FoodBeverageKitchen, policy => policy.RequireRole(PmsRoles.FoodBeverageKitchen));
    options.AddPolicy(PmsPolicies.Sales, policy => policy.RequireRole(PmsRoles.Sales));
    options.AddPolicy(PmsPolicies.Banquet, policy => policy.RequireRole(PmsRoles.Banquet));
    options.AddPolicy(PmsPolicies.Revenue, policy => policy.RequireRole(PmsRoles.Revenue));
    options.AddPolicy(PmsPolicies.BookingEngineManagement, policy => policy.RequireRole(PmsRoles.BookingEngineManagement));
    options.AddPolicy(PmsPolicies.GuestPortalManagement, policy => policy.RequireRole(PmsRoles.GuestPortalManagement));
    options.AddPolicy(PmsPolicies.InventoryPurchasing, policy => policy.RequireRole(PmsRoles.InventoryPurchasing));
    options.AddPolicy(PmsPolicies.Reports, policy => policy.RequireRole(PmsRoles.Reports));
    options.AddPolicy(PmsPolicies.ReportAdministration, policy => policy.RequireRole(PmsRoles.ReportAdministration));
    options.AddPolicy(PmsPolicies.ManagementAI, policy => policy.RequireRole(PmsRoles.ManagementAI));
    options.AddPolicy(PmsPolicies.AIIntegrationSettings, policy => policy.RequireRole(PmsRoles.AIIntegrationSettings));
    options.AddPolicy(PmsPolicies.SystemManagement, policy => policy.RequireRole(PmsRoles.SystemManagement));
    options.AddPolicy(PmsPolicies.SystemAdministration, policy => policy.RequireRole(PmsRoles.SystemAdministration));
    options.AddPolicy(PmsPolicies.PrintableDocuments, policy => policy.RequireRole(PmsRoles.PrintableDocuments));
    options.AddPolicy(PmsPolicies.ClientDemo, policy => policy.RequireRole(PmsRoles.ClientDemo));
    options.AddPolicy(PmsPolicies.LaborCosting, policy => policy.RequireRole(PmsRoles.LaborCosting));
    options.AddPolicy(PmsPolicies.ExecutiveReporting, policy => policy.RequireRole(PmsRoles.ExecutiveReporting));
    options.AddPolicy(PmsPolicies.ExecutiveManagement, policy => policy.RequireRole(PmsRoles.ExecutiveManagement));
    options.AddPolicy(PmsPolicies.GroupManagement, policy => policy.RequireRole(PmsRoles.GroupManagement));
    options.AddPolicy(PmsPolicies.GroupFinance, policy => policy.RequireRole(PmsRoles.GroupFinance));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Index");
    options.Conventions.AuthorizePage("/Privacy");
    options.Conventions.AuthorizeFolder("/Admin", PmsPolicies.AdminSetup);
    options.Conventions.AuthorizeFolder("/FrontOffice", PmsPolicies.FrontOffice);
    options.Conventions.AuthorizeFolder("/Housekeeping", PmsPolicies.Housekeeping);
    options.Conventions.AuthorizeFolder("/Finance", PmsPolicies.Finance);
    options.Conventions.AuthorizeFolder("/Finance/Approvals", PmsPolicies.FinanceApprovals);
    options.Conventions.AuthorizeFolder("/Accounting", PmsPolicies.FinanceApprovals);
    options.Conventions.AuthorizeFolder("/Labor", PmsPolicies.LaborCosting);
    options.Conventions.AuthorizeFolder("/Executive", PmsPolicies.ExecutiveReporting);
    options.Conventions.AuthorizeFolder("/AccountsReceivable", PmsPolicies.AccountsReceivable);
    options.Conventions.AuthorizeFolder("/FoodBeverage", PmsPolicies.FoodBeverageService);
    options.Conventions.AuthorizeFolder("/FoodBeverageKitchen", PmsPolicies.FoodBeverageKitchen);
    options.Conventions.AuthorizeFolder("/Sales", PmsPolicies.Sales);
    options.Conventions.AuthorizeFolder("/Banquet", PmsPolicies.Banquet);
    options.Conventions.AuthorizeFolder("/Revenue", PmsPolicies.Revenue);
    options.Conventions.AuthorizeFolder("/BookingManagement", PmsPolicies.BookingEngineManagement);
    options.Conventions.AllowAnonymousToFolder("/Booking");
    options.Conventions.AuthorizeFolder("/GuestPortalManagement", PmsPolicies.GuestPortalManagement);
    options.Conventions.AllowAnonymousToFolder("/GuestPortal");
    options.Conventions.AuthorizeFolder("/Inventory", PmsPolicies.InventoryPurchasing);
    options.Conventions.AuthorizeFolder("/Purchasing", PmsPolicies.InventoryPurchasing);
    options.Conventions.AuthorizeFolder("/Reports", PmsPolicies.Reports);
    options.Conventions.AuthorizeFolder("/Groups", PmsPolicies.GroupManagement);
    options.Conventions.AuthorizeFolder("/ManagementAI", PmsPolicies.ManagementAI);
    options.Conventions.AuthorizeFolder("/ManagementAI/Settings", PmsPolicies.AIIntegrationSettings);
    options.Conventions.AuthorizeFolder("/System/AuditLogs", PmsPolicies.SystemManagement);
    options.Conventions.AuthorizeFolder("/System/ErrorLogs", PmsPolicies.SystemAdministration);
    options.Conventions.AuthorizeFolder("/System/Notifications", PmsPolicies.SystemManagement);
    options.Conventions.AuthorizeFolder("/System/Settings", PmsPolicies.SystemManagement);
    options.Conventions.AuthorizeFolder("/System/HealthCheck", PmsPolicies.SystemManagement);
    options.Conventions.AuthorizeFolder("/System/RouteReadiness", PmsPolicies.SystemManagement);
    options.Conventions.AuthorizeFolder("/System/WorkflowReadiness", PmsPolicies.SystemManagement);
    options.Conventions.AuthorizeFolder("/System/RoleAccessReadiness", PmsPolicies.SystemManagement);
    options.Conventions.AuthorizeFolder("/System/ReportReadiness", PmsPolicies.SystemManagement);
    options.Conventions.AuthorizeFolder("/System/PilotLaunch", PmsPolicies.SystemManagement);
    options.Conventions.AuthorizeFolder("/System/DataValidationIssues", PmsPolicies.SystemManagement);
    options.Conventions.AuthorizeFolder("/System/QAChecklist", PmsPolicies.SystemManagement);
    options.Conventions.AuthorizeFolder("/System/ModuleQA", PmsPolicies.SystemManagement);
    options.Conventions.AuthorizeFolder("/System/DemoPresentation", PmsPolicies.SystemManagement);
    options.Conventions.AuthorizeFolder("/System/DocumentTemplateSettings", PmsPolicies.SystemManagement);
    options.Conventions.AuthorizeFolder("/System/ClientDemoPackage", PmsPolicies.ClientDemo);
    options.Conventions.AuthorizeFolder("/System/DemoWorkflowLauncher", PmsPolicies.ClientDemo);
    options.Conventions.AuthorizeFolder("/Documents", PmsPolicies.PrintableDocuments);
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    try
    {
        await IdentitySeedData.SeedAsync(scope.ServiceProvider, app.Configuration);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("StartupDataSeed");
        var requireStartupSeed = app.Environment.IsDevelopment()
            || app.Configuration.GetValue("Startup:RequireIdentitySeed", false);

        if (requireStartupSeed)
        {
            logger.LogCritical(ex, "Required startup data seeding failed. The application cannot start until database connectivity, migrations, and bootstrap configuration are corrected.");
            throw;
        }

        logger.LogError(ex, "Startup data seeding failed, but the application will continue because Startup:RequireIdentitySeed is disabled for this environment. Verify database connectivity and run setup seeding after startup.");
    }
}

if (args.Contains("--seed-full-demo", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("MaintenanceSeed");
    var demoSeeder = scope.ServiceProvider.GetRequiredService<DemoDataSeederService>();
    var result = await demoSeeder.SeedFullDemoDatasetAsync("MaintenanceSeed");

    logger.LogInformation(
        "Full demo dataset seed completed. Inserted/updated {Inserted} record(s). Messages: {Messages}",
        result.Inserted,
        string.Join(" | ", result.Messages));
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseHsts();
}

app.UseExceptionHandler("/Error");

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<SystemErrorLoggingMiddleware>();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
