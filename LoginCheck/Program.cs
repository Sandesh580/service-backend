using System.Text;
using LoginCheck.Application.Interfaces;
using LoginCheck.Domain.Entities;
using LoginCheck.Infrastructure.Data;
using LoginCheck.Infrastructure.Identity;
using LoginCheck.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=LoginCheckDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";
var isMySql = Uri.TryCreate(connectionString, UriKind.Absolute, out var databaseUri)
    && databaseUri.Scheme.Equals("mysql", StringComparison.OrdinalIgnoreCase);
if (isMySql)
{
    var credentials = databaseUri!.UserInfo.Split(':', 2);
    if (credentials.Length != 2)
        throw new InvalidOperationException("The hosted MySQL URL must include a username and password.");

    connectionString = new MySqlConnectionStringBuilder
    {
        Server = databaseUri.Host,
        Port = checked((uint)databaseUri.Port),
        Database = Uri.UnescapeDataString(databaseUri.AbsolutePath.Trim('/')),
        UserID = Uri.UnescapeDataString(credentials[0]),
        Password = Uri.UnescapeDataString(credentials[1]),
        SslMode = MySqlSslMode.Required
    }.ConnectionString;
}
var useInMemoryDevelopmentFallback = false;
var allowInMemoryDevelopmentFallback = builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue<bool>("Database:AllowInMemoryFallback");

try
{
    if (!isMySql)
    {
        var masterConnectionString = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString;
        using var connection = new SqlConnection(masterConnectionString);
        connection.Open();
    }
}
catch (SqlException exception) when (allowInMemoryDevelopmentFallback)
{
    useInMemoryDevelopmentFallback = true;
    Console.WriteLine($"SQL Server is unavailable; using the development-only in-memory database. {exception.Message}");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useInMemoryDevelopmentFallback)
        options.UseInMemoryDatabase("LoginCheckDevelopmentFallback");
    else if (isMySql)
        options.UseMySQL(connectionString);
    else
        options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ITechnicianService, TechnicianService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "default-secret-key-for-dev-only-1234567890"))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Create DB + tables if not exist, then seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
    if (!isMySql)
    {
    dbContext.Database.ExecuteSqlRaw("""
        IF COL_LENGTH('dbo.Users', 'Specialties') IS NULL ALTER TABLE dbo.Users ADD Specialties nvarchar(300) NOT NULL CONSTRAINT DF_Users_Specialties DEFAULT N'';
        IF COL_LENGTH('dbo.Users', 'EducationQualification') IS NULL ALTER TABLE dbo.Users ADD EducationQualification nvarchar(200) NOT NULL CONSTRAINT DF_Users_EducationQualification DEFAULT N'';
        IF COL_LENGTH('dbo.Users', 'VocationalTraining') IS NULL ALTER TABLE dbo.Users ADD VocationalTraining nvarchar(500) NOT NULL CONSTRAINT DF_Users_VocationalTraining DEFAULT N'';
        IF COL_LENGTH('dbo.Users', 'PermanentAddress') IS NULL ALTER TABLE dbo.Users ADD PermanentAddress nvarchar(1000) NOT NULL CONSTRAINT DF_Users_PermanentAddress DEFAULT N'';
        IF COL_LENGTH('dbo.Users', 'CurrentAddress') IS NULL ALTER TABLE dbo.Users ADD CurrentAddress nvarchar(1000) NOT NULL CONSTRAINT DF_Users_CurrentAddress DEFAULT N'';
        IF COL_LENGTH('dbo.Users', 'BloodGroup') IS NULL ALTER TABLE dbo.Users ADD BloodGroup nvarchar(10) NOT NULL CONSTRAINT DF_Users_BloodGroup DEFAULT N'';
        IF COL_LENGTH('dbo.Users', 'PastExperience') IS NULL ALTER TABLE dbo.Users ADD PastExperience nvarchar(2000) NOT NULL CONSTRAINT DF_Users_PastExperience DEFAULT N'';
        IF COL_LENGTH('dbo.Users', 'AvatarKey') IS NULL ALTER TABLE dbo.Users ADD AvatarKey nvarchar(32) NOT NULL CONSTRAINT DF_Users_AvatarKey DEFAULT N'avatar-1';
        IF COL_LENGTH('dbo.Users', 'Address') IS NULL ALTER TABLE dbo.Users ADD Address nvarchar(1000) NOT NULL CONSTRAINT DF_Users_Address DEFAULT N'';
        IF COL_LENGTH('dbo.ServiceRequests', 'CustomerUserId') IS NULL ALTER TABLE dbo.ServiceRequests ADD CustomerUserId int NULL;
        IF COL_LENGTH('dbo.ServiceRequests', 'AssignedTechnicianId') IS NULL ALTER TABLE dbo.ServiceRequests ADD AssignedTechnicianId int NULL;
        IF COL_LENGTH('dbo.ServiceRequests', 'ProblemDescription') IS NULL ALTER TABLE dbo.ServiceRequests ADD ProblemDescription nvarchar(1000) NOT NULL CONSTRAINT DF_ServiceRequests_ProblemDescription DEFAULT N'';
        IF COL_LENGTH('dbo.ServiceRequests', 'ApplianceBrand') IS NULL ALTER TABLE dbo.ServiceRequests ADD ApplianceBrand nvarchar(100) NOT NULL CONSTRAINT DF_ServiceRequests_ApplianceBrand DEFAULT N'';
        IF COL_LENGTH('dbo.ServiceRequests', 'ApplianceModel') IS NULL ALTER TABLE dbo.ServiceRequests ADD ApplianceModel nvarchar(100) NOT NULL CONSTRAINT DF_ServiceRequests_ApplianceModel DEFAULT N'';
        IF COL_LENGTH('dbo.ServiceRequests', 'SerialNumber') IS NULL ALTER TABLE dbo.ServiceRequests ADD SerialNumber nvarchar(100) NOT NULL CONSTRAINT DF_ServiceRequests_SerialNumber DEFAULT N'';
        IF COL_LENGTH('dbo.ServiceRequests', 'CustomerPhone') IS NULL ALTER TABLE dbo.ServiceRequests ADD CustomerPhone nvarchar(40) NOT NULL CONSTRAINT DF_ServiceRequests_CustomerPhone DEFAULT N'';
        IF COL_LENGTH('dbo.ServiceRequests', 'ServiceAddress') IS NULL ALTER TABLE dbo.ServiceRequests ADD ServiceAddress nvarchar(500) NOT NULL CONSTRAINT DF_ServiceRequests_ServiceAddress DEFAULT N'';
        IF COL_LENGTH('dbo.ServiceRequests', 'Latitude') IS NULL ALTER TABLE dbo.ServiceRequests ADD Latitude float NULL;
        IF COL_LENGTH('dbo.ServiceRequests', 'Longitude') IS NULL ALTER TABLE dbo.ServiceRequests ADD Longitude float NULL;
        IF COL_LENGTH('dbo.ServiceRequests', 'PreferredServiceDate') IS NULL ALTER TABLE dbo.ServiceRequests ADD PreferredServiceDate datetime2 NULL;
        IF COL_LENGTH('dbo.ServiceRequests', 'NextVisitDate') IS NULL ALTER TABLE dbo.ServiceRequests ADD NextVisitDate datetime2 NULL;
        IF COL_LENGTH('dbo.ServiceRequests', 'IssueCategory') IS NULL ALTER TABLE dbo.ServiceRequests ADD IssueCategory nvarchar(100) NOT NULL CONSTRAINT DF_ServiceRequests_IssueCategory DEFAULT N'';
        IF COL_LENGTH('dbo.ServiceRequests', 'TechnicianNote') IS NULL ALTER TABLE dbo.ServiceRequests ADD TechnicianNote nvarchar(2000) NOT NULL CONSTRAINT DF_ServiceRequests_TechnicianNote DEFAULT N'';
        IF COL_LENGTH('dbo.ServiceRequests', 'RejectionReason') IS NULL ALTER TABLE dbo.ServiceRequests ADD RejectionReason nvarchar(1000) NOT NULL CONSTRAINT DF_ServiceRequests_RejectionReason DEFAULT N'';
        IF COL_LENGTH('dbo.ServiceRequests', 'TechnicianActionAt') IS NULL ALTER TABLE dbo.ServiceRequests ADD TechnicianActionAt datetime2 NULL;
        IF COL_LENGTH('dbo.ServiceRequests', 'AmountPaid') IS NULL ALTER TABLE dbo.ServiceRequests ADD AmountPaid decimal(10,2) NULL;
        IF COL_LENGTH('dbo.ServiceRequests', 'ManagerNote') IS NULL ALTER TABLE dbo.ServiceRequests ADD ManagerNote nvarchar(2000) NOT NULL CONSTRAINT DF_ServiceRequests_ManagerNote DEFAULT N'';
        IF OBJECT_ID('dbo.ServiceNotifications', 'U') IS NULL CREATE TABLE dbo.ServiceNotifications (Id int IDENTITY(1,1) NOT NULL PRIMARY KEY, RecipientUserId int NOT NULL, ServiceRequestId int NULL, RequestNumber nvarchar(20) NOT NULL DEFAULT N'', Title nvarchar(160) NOT NULL, Message nvarchar(1000) NOT NULL, CreatedAt datetime2 NOT NULL, IsRead bit NOT NULL CONSTRAINT DF_ServiceNotifications_IsRead DEFAULT 0);
        IF OBJECT_ID('dbo.InventoryParts', 'U') IS NULL CREATE TABLE dbo.InventoryParts (Id int IDENTITY(1,1) NOT NULL PRIMARY KEY, Sku nvarchar(40) NOT NULL, Name nvarchar(150) NOT NULL, ApplianceType nvarchar(80) NOT NULL, Description nvarchar(500) NOT NULL DEFAULT N'', QuantityInStock int NOT NULL, UnitPrice decimal(10,2) NOT NULL, CONSTRAINT UQ_InventoryParts_Sku UNIQUE (Sku));
        """);
    dbContext.Database.ExecuteSqlRaw("""
        IF OBJECT_ID('dbo.ServiceRequestParts', 'U') IS NULL CREATE TABLE dbo.ServiceRequestParts (Id int IDENTITY(1,1) NOT NULL PRIMARY KEY, ServiceRequestId int NOT NULL, InventoryPartId int NOT NULL, Quantity int NOT NULL, AssignedAt datetime2 NOT NULL, CONSTRAINT FK_ServiceRequestParts_ServiceRequests FOREIGN KEY (ServiceRequestId) REFERENCES dbo.ServiceRequests(Id) ON DELETE CASCADE, CONSTRAINT FK_ServiceRequestParts_InventoryParts FOREIGN KEY (InventoryPartId) REFERENCES dbo.InventoryParts(Id), CONSTRAINT UQ_ServiceRequestParts_Request_Part UNIQUE (ServiceRequestId, InventoryPartId), CONSTRAINT CK_ServiceRequestParts_Quantity CHECK (Quantity > 0));
        """);
    // Run references to the newly-added columns in a later batch; SQL Server compiles each batch before executing it.
    dbContext.Database.ExecuteSqlRaw("""
        IF COL_LENGTH('dbo.Users', 'Specialties') IS NULL ALTER TABLE dbo.Users ADD Specialties nvarchar(300) NOT NULL CONSTRAINT DF_Users_Specialties DEFAULT N'';
        INSERT INTO dbo.InventoryParts (Sku, Name, ApplianceType, Description, QuantityInStock, UnitPrice)
        SELECT seed.Sku, seed.Name, seed.ApplianceType, seed.Description, seed.QuantityInStock, seed.UnitPrice
        FROM (VALUES
            (N'FRG-WF-001', N'Refrigerator water filter', N'Refrigerator', N'Inline replacement filter for compatible refrigerator models.', 12, CAST(24.99 AS decimal(10,2))),
            (N'FRG-TH-002', N'Refrigerator thermostat', N'Refrigerator', N'Temperature control thermostat assembly.', 7, CAST(39.50 AS decimal(10,2))),
            (N'WASH-DP-001', N'Washer drain pump', N'Washing machine', N'Drain pump assembly for front and top load washers.', 8, CAST(58.00 AS decimal(10,2))),
            (N'WASH-IV-002', N'Washer inlet valve', N'Washing machine', N'Dual water inlet valve assembly.', 10, CAST(31.75 AS decimal(10,2))),
            (N'DISH-CMP-001', N'Dishwasher circulation pump', N'Dishwasher', N'Wash circulation pump and motor assembly.', 4, CAST(112.00 AS decimal(10,2))),
            (N'DISH-DL-002', N'Dishwasher door latch', N'Dishwasher', N'Door latch and switch replacement kit.', 9, CAST(27.25 AS decimal(10,2))),
            (N'DRY-HE-001', N'Dryer heating element', N'Dryer', N'Electric dryer heating element assembly.', 6, CAST(76.00 AS decimal(10,2))),
            (N'AC-CAP-001', N'Air conditioner run capacitor', N'Air conditioner', N'Dual run capacitor for common residential AC units.', 14, CAST(22.50 AS decimal(10,2))),
            (N'OVN-IGN-001', N'Oven igniter', N'Oven', N'Universal gas oven igniter replacement part.', 5, CAST(44.00 AS decimal(10,2)))
        ) AS seed(Sku, Name, ApplianceType, Description, QuantityInStock, UnitPrice)
        WHERE NOT EXISTS (SELECT 1 FROM dbo.InventoryParts existing WHERE existing.Sku = seed.Sku);
        UPDATE request SET AssignedTechnicianId = technician.Id FROM dbo.ServiceRequests AS request INNER JOIN dbo.Users AS technician ON technician.Role = N'Technician' AND technician.UserName = request.AssignedTechnician WHERE request.AssignedTechnicianId IS NULL;
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ServiceRequests_CustomerUserId' AND object_id = OBJECT_ID('dbo.ServiceRequests')) CREATE INDEX IX_ServiceRequests_CustomerUserId ON dbo.ServiceRequests(CustomerUserId);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ServiceRequests_AssignedTechnicianId' AND object_id = OBJECT_ID('dbo.ServiceRequests')) CREATE INDEX IX_ServiceRequests_AssignedTechnicianId ON dbo.ServiceRequests(AssignedTechnicianId);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ServiceNotifications_RecipientUserId_CreatedAt' AND object_id = OBJECT_ID('dbo.ServiceNotifications')) CREATE INDEX IX_ServiceNotifications_RecipientUserId_CreatedAt ON dbo.ServiceNotifications(RecipientUserId, CreatedAt DESC);
        """);
    }
    if (isMySql)
        SeedInventoryParts(dbContext);
    SeedData(dbContext);
}

app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));
    app.MapGet("/swagger", () => Results.Redirect("/scalar/v1"));
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void SeedData(ApplicationDbContext db)
{
    if (db.ServiceRequests.Any()) return; // already seeded

    var requests = new List<ServiceRequest>
    {
        // New Requests (4)
        new() { RequestNumber = "2401", CustomerName = "Liam M.", Appliance = "Bosch Dishwasher",   Issue = "Not draining",          Status = "PendingApproval", Priority = "High",   AssignedTechnician = "PMD",  Date = DateTime.UtcNow.AddDays(-1) },
        new() { RequestNumber = "2402", CustomerName = "Sarah J.", Appliance = "LG Refrigerator",    Issue = "Not cooling",           Status = "New",             Priority = "Medium", AssignedTechnician = "PMD",  Date = DateTime.UtcNow.AddDays(-2) },
        new() { RequestNumber = "2403", CustomerName = "Tom R.",   Appliance = "Samsung Washer",     Issue = "Leaking water",         Status = "New",             Priority = "High",   AssignedTechnician = null,   Date = DateTime.UtcNow.AddDays(-2) },
        new() { RequestNumber = "2404", CustomerName = "Emma K.",  Appliance = "GE Oven",            Issue = "Not heating",           Status = "New",             Priority = "Low",    AssignedTechnician = null,   Date = DateTime.UtcNow.AddDays(-3) },

        // Awaiting Parts (5)
        new() { RequestNumber = "2405", CustomerName = "Sarah J.", Appliance = "Whirlpool Dryer",    Issue = "Won't start",           Status = "AwaitingParts",   Priority = "Medium", AssignedTechnician = "PMD",  Date = DateTime.UtcNow.AddDays(-4) },
        new() { RequestNumber = "2406", CustomerName = "Mike D.",  Appliance = "KitchenAid Mixer",   Issue = "Motor burnt",           Status = "AwaitingParts",   Priority = "Low",    AssignedTechnician = "SIGN", Date = DateTime.UtcNow.AddDays(-4) },
        new() { RequestNumber = "2407", CustomerName = "Julia W.", Appliance = "Miele Dishwasher",   Issue = "Not cleaning",          Status = "AwaitingParts",   Priority = "Medium", AssignedTechnician = "PMD",  Date = DateTime.UtcNow.AddDays(-5) },
        new() { RequestNumber = "2408", CustomerName = "Alex B.",  Appliance = "Frigidaire Fridge",  Issue = "Ice maker broken",      Status = "AwaitingParts",   Priority = "Low",    AssignedTechnician = null,   Date = DateTime.UtcNow.AddDays(-5) },
        new() { RequestNumber = "2409", CustomerName = "Nina S.",  Appliance = "Bosch Washer",       Issue = "Vibrating badly",       Status = "AwaitingParts",   Priority = "High",   AssignedTechnician = "PMD",  Date = DateTime.UtcNow.AddDays(-6) },

        // Scheduled (7)
        new() { RequestNumber = "2410", CustomerName = "Sarah J.", Appliance = "Samsung Fridge",     Issue = "Temperature too warm",  Status = "Scheduled",       Priority = "High",   AssignedTechnician = "PMD",  Date = DateTime.UtcNow.AddDays(1) },
        new() { RequestNumber = "2411", CustomerName = "Michael B.", Appliance = "Whirlpool Washer", Issue = "Loud noise spinning",   Status = "Scheduled",       Priority = "Medium", AssignedTechnician = "PMD",  Date = DateTime.UtcNow.AddDays(1) },
        new() { RequestNumber = "2412", CustomerName = "Chris P.", Appliance = "LG Dryer",           Issue = "Not drying clothes",    Status = "Scheduled",       Priority = "Medium", AssignedTechnician = "SIGN", Date = DateTime.UtcNow.AddDays(2) },
        new() { RequestNumber = "2413", CustomerName = "Laura F.", Appliance = "GE Dishwasher",      Issue = "Door won't latch",      Status = "Scheduled",       Priority = "Low",    AssignedTechnician = null,   Date = DateTime.UtcNow.AddDays(2) },
        new() { RequestNumber = "2414", CustomerName = "David C.", Appliance = "Bosch Oven",         Issue = "Temperature off",       Status = "Scheduled",       Priority = "Medium", AssignedTechnician = "PMD",  Date = DateTime.UtcNow.AddDays(3) },
        new() { RequestNumber = "2415", CustomerName = "Anna M.",  Appliance = "KitchenAid Fridge",  Issue = "Water dispenser leaks", Status = "Scheduled",       Priority = "Low",    AssignedTechnician = "PMD",  Date = DateTime.UtcNow.AddDays(3) },
        new() { RequestNumber = "2416", CustomerName = "Ryan T.",  Appliance = "Samsung Dryer",      Issue = "Heating element bad",   Status = "Scheduled",       Priority = "High",   AssignedTechnician = "SIGN", Date = DateTime.UtcNow.AddDays(4) },

        // In Progress (12)
        new() { RequestNumber = "2417", CustomerName = "Mich W.",  Appliance = "Whirlpool Fridge",   Issue = "Compressor noise",      Status = "InProgress",      Priority = "High",   AssignedTechnician = "PMD",  Date = DateTime.UtcNow },
        new() { RequestNumber = "2418", CustomerName = "Lacios M.", Appliance = "LG Washer",         Issue = "Not spinning",          Status = "InProgress",      Priority = "Medium", AssignedTechnician = "SIGN", Date = DateTime.UtcNow },
        new() { RequestNumber = "2419", CustomerName = "Carol H.", Appliance = "Bosch Dryer",        Issue = "Overheating",           Status = "InProgress",      Priority = "High",   AssignedTechnician = "PMD",  Date = DateTime.UtcNow.AddDays(-1) },
        new() { RequestNumber = "2420", CustomerName = "Steve G.", Appliance = "Samsung Oven",       Issue = "One burner not working", Status = "InProgress",     Priority = "Low",    AssignedTechnician = null,   Date = DateTime.UtcNow.AddDays(-1) },
        new() { RequestNumber = "2421", CustomerName = "Lucy A.",  Appliance = "GE Refrigerator",    Issue = "Door seal broken",      Status = "InProgress",      Priority = "Medium", AssignedTechnician = "PMD",  Date = DateTime.UtcNow.AddDays(-1) },
        new() { RequestNumber = "2422", CustomerName = "Mark B.",  Appliance = "Miele Washer",       Issue = "Error code E5",         Status = "InProgress",      Priority = "High",   AssignedTechnician = "SIGN", Date = DateTime.UtcNow.AddDays(-1) },
        new() { RequestNumber = "2423", CustomerName = "Fiona K.", Appliance = "Frigidaire Dryer",   Issue = "Drum not turning",      Status = "InProgress",      Priority = "Medium", AssignedTechnician = "PMD",  Date = DateTime.UtcNow.AddDays(-2) },
        new() { RequestNumber = "2424", CustomerName = "Jack P.",  Appliance = "LG Dishwasher",      Issue = "Water not draining",    Status = "InProgress",      Priority = "High",   AssignedTechnician = "PMD",  Date = DateTime.UtcNow.AddDays(-2) },
        new() { RequestNumber = "2425", CustomerName = "Olivia R.", Appliance = "Whirlpool Oven",    Issue = "Oven door cracked",     Status = "InProgress",      Priority = "Low",    AssignedTechnician = "SIGN", Date = DateTime.UtcNow.AddDays(-2) },
        new() { RequestNumber = "2426", CustomerName = "Brian S.", Appliance = "Bosch Refrigerator", Issue = "Not making ice",        Status = "InProgress",      Priority = "Medium", AssignedTechnician = "PMD",  Date = DateTime.UtcNow.AddDays(-3) },
        new() { RequestNumber = "2427", CustomerName = "Nancy T.", Appliance = "Samsung Washer",     Issue = "Won't spin fast",       Status = "InProgress",      Priority = "Low",    AssignedTechnician = null,   Date = DateTime.UtcNow.AddDays(-3) },
        new() { RequestNumber = "2428", CustomerName = "Paul L.",  Appliance = "GE Dryer",           Issue = "Timer broken",          Status = "InProgress",      Priority = "Medium", AssignedTechnician = "PMD",  Date = DateTime.UtcNow.AddDays(-3) },

        // Completed (135 — adding representative sample)
    };

    // Add 135 completed jobs
    for (int i = 0; i < 135; i++)
    {
        requests.Add(new ServiceRequest
        {
            RequestNumber = (2300 + i).ToString(),
            CustomerName = $"Customer {i + 1}",
            Appliance = new[] { "Samsung Fridge", "LG Washer", "Bosch Dryer", "GE Oven", "Whirlpool Dishwasher" }[i % 5],
            Issue = new[] { "Not cooling", "Leaking", "Not heating", "Noisy", "Door broken" }[i % 5],
            Status = "Completed",
            Priority = new[] { "High", "Medium", "Low" }[i % 3],
            AssignedTechnician = new[] { "PMD", "SIGN", "TECH1" }[i % 3],
            Date = DateTime.UtcNow.AddDays(-(i + 5))
        });
    }

    db.ServiceRequests.AddRange(requests);
    db.SaveChanges();
}

static void SeedInventoryParts(ApplicationDbContext db)
{
    if (db.InventoryParts.Any()) return;

    db.InventoryParts.AddRange(
        new InventoryPart { Sku = "FRG-WF-001", Name = "Refrigerator water filter", ApplianceType = "Refrigerator", Description = "Inline replacement filter for compatible refrigerator models.", QuantityInStock = 12, UnitPrice = 24.99m },
        new InventoryPart { Sku = "FRG-TH-002", Name = "Refrigerator thermostat", ApplianceType = "Refrigerator", Description = "Temperature control thermostat assembly.", QuantityInStock = 7, UnitPrice = 39.50m },
        new InventoryPart { Sku = "WASH-DP-001", Name = "Washer drain pump", ApplianceType = "Washing machine", Description = "Drain pump assembly for front and top load washers.", QuantityInStock = 8, UnitPrice = 58.00m },
        new InventoryPart { Sku = "WASH-IV-002", Name = "Washer inlet valve", ApplianceType = "Washing machine", Description = "Dual water inlet valve assembly.", QuantityInStock = 10, UnitPrice = 31.75m },
        new InventoryPart { Sku = "DISH-CMP-001", Name = "Dishwasher circulation pump", ApplianceType = "Dishwasher", Description = "Wash circulation pump and motor assembly.", QuantityInStock = 4, UnitPrice = 112.00m },
        new InventoryPart { Sku = "DISH-DL-002", Name = "Dishwasher door latch", ApplianceType = "Dishwasher", Description = "Door latch and switch replacement kit.", QuantityInStock = 9, UnitPrice = 27.25m },
        new InventoryPart { Sku = "DRY-HE-001", Name = "Dryer heating element", ApplianceType = "Dryer", Description = "Electric dryer heating element assembly.", QuantityInStock = 6, UnitPrice = 76.00m },
        new InventoryPart { Sku = "AC-CAP-001", Name = "Air conditioner run capacitor", ApplianceType = "Air conditioner", Description = "Dual run capacitor for common residential AC units.", QuantityInStock = 14, UnitPrice = 22.50m },
        new InventoryPart { Sku = "OVN-IGN-001", Name = "Oven igniter", ApplianceType = "Oven", Description = "Universal gas oven igniter replacement part.", QuantityInStock = 5, UnitPrice = 44.00m });
    db.SaveChanges();
}
