using chatbackend.Data;
using chatbackend.Interfaces;
using chatbackend.Models;
using chatbackend.Repository;
using chatbackend.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information) // Reduce Microsoft's log spam
    .Enrich.FromLogContext() // Enrich logs with context properties (e.g., request ID)
    .WriteTo.File(
        path: "Logs/log-.txt", // Log file path
        rollingInterval: RollingInterval.Day, // Create a new log file each day
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}" // Log format
    )
    .CreateLogger();

builder.Services.AddSerilog();


builder.Services.AddControllers();
builder.Services.AddOpenApi();


// This is the connection to postgresql database
// Connection String needs to be customized depending on sql database
builder.Services.AddDbContext<ApplicationDBContext>(options =>{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// This handles object cycles (circular references) in JSON serialization
// JSON serialization is turning data structures into JSON strings
builder.Services.AddControllers().AddNewtonsoftJson(options => {
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
});

builder.Services.AddIdentity<User, IdentityRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
}).AddEntityFrameworkStores<ApplicationDBContext>();

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme =
    options.DefaultForbidScheme =
    options.DefaultScheme =
    options.DefaultSignInScheme =
    options.DefaultChallengeScheme =
    options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:Audience"],
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigningKey"])
        )
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Add authentication token from query string
            var accessToken = context.Request.Query["accessToken"];

            // If the request is for our hub...
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/chatHub")))
            {
                // Read the token out of the query string
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserOwnsResource", policy =>
        policy.RequireClaim("userid"));
});


// These are dependency injections, they allow for implicit construction of interfaced models
// Controllers utilize implicit declarations. For example:
// Instead of creating StockRepository, stock controller creates IStockRepository
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthorizationService, MyAuthorizationService>();
builder.Services.AddScoped<IUrlHelper>(provider =>
{
    var actionContext = provider.GetRequiredService<IActionContextAccessor>().ActionContext;
    var factory = provider.GetRequiredService<IUrlHelperFactory>();
    return factory.GetUrlHelper(actionContext);
});
builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // This allows for swagger connection. v1.json is the default openapi json.
    app.UseSwaggerUI(c =>{
        c.SwaggerEndpoint("/openapi/v1.json","Demo Api");
    });
}

app.UseHttpsRedirection();

// For JWT
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