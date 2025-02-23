using chatbackend.Data;
using chatbackend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

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
});

// These are dependency injections, they allow for implicit construction of interfaced models
// Controllers utilize implicit declarations. For example:
// Instead of creating StockRepository, stock controller creates IStockRepository

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