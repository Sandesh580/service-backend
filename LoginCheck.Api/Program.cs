using System.Text;
using LoginCheck.Application.Interfaces;
using LoginCheck.Infrastructure.Data;
using LoginCheck.Infrastructure.Identity;
using LoginCheck.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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

// Aiven and other hosted services commonly provide a mysql:// URI. Convert it
// to Connector/NET's connection-string format while keeping credentials out of source.
if (Uri.TryCreate(connectionString, UriKind.Absolute, out var databaseUri)
    && databaseUri.Scheme.Equals("mysql", StringComparison.OrdinalIgnoreCase))
{
    var credentials = databaseUri.UserInfo.Split(':', 2);
    if (credentials.Length != 2)
        throw new InvalidOperationException("The hosted MySQL URL must include a username and password.");

    var mysql = new MySqlConnectionStringBuilder
    {
        Server = databaseUri.Host,
        Port = checked((uint)databaseUri.Port),
        Database = Uri.UnescapeDataString(databaseUri.AbsolutePath.Trim('/')),
        UserID = Uri.UnescapeDataString(credentials[0]),
        Password = Uri.UnescapeDataString(credentials[1]),
        SslMode = MySqlSslMode.Required
    };
    connectionString = mysql.ConnectionString;
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

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

// Automatically creates database and tables if they don't exist yet; does nothing if they already exist
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
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
