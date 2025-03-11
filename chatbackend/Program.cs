using System.Text;
using chatbackend.Data;
using chatbackend.Interfaces;
using chatbackend.Models;
using chatbackend.Repository;
using chatbackend.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Logging (Serilog)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.File(
        path: "Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();
builder.Services.AddSerilog();


// 2. Add Basic Services (Controllers, OpenAPI)
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
});

builder.Services.AddOpenApi();


// 3. Configure Database Context (PostgreSQL)
builder.Services.AddDbContext<ApplicationDBContext>((serviceProvider, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));

    var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDBContext>>();
    options.EnableSensitiveDataLogging(); // NEVER in production
}, ServiceLifetime.Scoped);



// 4. Configure Identity
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
})
    .AddEntityFrameworkStores<ApplicationDBContext>();

// 5. Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultForbidScheme =
    options.DefaultScheme =
    options.DefaultSignInScheme =
    options.DefaultChallengeScheme = options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:Audience"],
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigningKey"]))
    };
});

// 6. Register Application Services (Scoped, Singleton, etc.)
builder.Services.AddScoped<IMessageService, MessageService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserOwnsResource", policy => policy.RequireClaim("userid"));
});

builder.Services.AddScoped(provider =>
{
    var logger = provider.GetRequiredService<ILogger<FileSystemAccess>>();
    return new FileSystemAccess(logger, builder.Configuration["FileStorage:BaseFilePath"]);
});
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped(provider =>
{
    var logger = provider.GetRequiredService<ILogger<MyAuthorizationService>>();
    var urlHelperFactory = provider.GetRequiredService<IUrlHelperFactory>();
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    var config = provider.GetRequiredService<IConfiguration>();
    var context = provider.GetRequiredService<ApplicationDBContext>();

    return new MyAuthorizationService(
        context,
        logger,
        urlHelperFactory,
        httpContextAccessor,
        config);
});
// Inotify limit fix
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);


// Build the Application
var app = builder.Build();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "Demo Api");
    });
}

app.UseHttpsRedirection();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


/*
// Serve React static files
app.UseDefaultFiles(); // Automatically serves index.html
app.UseStaticFiles();  // Serves JS, CSS, images, etc.

app.MapControllers();

// Redirect all unknown routes to index.html for React routing
app.MapFallbackToFile("/index.html");

app.Run();
*/