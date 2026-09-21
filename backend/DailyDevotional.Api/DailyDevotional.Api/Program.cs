using DailyDevotional.Api.Data;
using DailyDevotional.Api.Models;
using DailyDevotional.Api.Services;
using DailyDevotional.Api.Services.IServices;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
  options.AddPolicy("Angular", policy =>
  {
    policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod();
  });
});

builder.Services.AddDbContext<AppDbContext>(options =>
options.UseNpgsql(
  builder.Configuration.GetConnectionString("DefaultConnection")
  ));

builder.Services
  .AddIdentityCore<ApplicationUser>()
  .AddEntityFrameworkStores<AppDbContext>();

var jwtKey = builder.Configuration["Authentication:Jwt:Key"];
var jwtIssuer = builder.Configuration["Authentication:Jwt:Issuer"];
var jwtAudience = builder.Configuration["Authentication:Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
  throw new InvalidOperationException(
      "Authentication:Jwt:Key, Authentication:Jwt:Issuer, and Authentication:Jwt:Audience must all be " +
      "configured. Set them with, e.g., 'dotnet user-secrets set \"Authentication:Jwt:Key\" \"<value>\"' " +
      "from the DailyDevotional.Api project directory.");
}

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

var authenticationBuilder = builder.Services
    .AddAuthentication(options =>
    {
      options.DefaultAuthenticateScheme =
          JwtBearerDefaults.AuthenticationScheme;

      options.DefaultChallengeScheme =
          JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtIssuer,

        ValidAudience = jwtAudience,

        IssuerSigningKey =
              new SymmetricSecurityKey(
                  Encoding.UTF8.GetBytes(jwtKey!)
              )
      };
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
  authenticationBuilder.AddGoogle(options =>
  {
    options.ClientId = googleClientId;
    options.ClientSecret = googleClientSecret;
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    options.Events.OnRedirectToAuthorizationEndpoint = context =>
    {
      context.Response.Redirect(context.RedirectUri + "&prompt=select_account");
      return Task.CompletedTask;
    };
  });
}

builder.Services.AddScoped<IJournalService, JournalService>();
builder.Services.AddScoped<IDailyReadingService, DailyReadingService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<IDailyReadingImportService, DailyReadingImportService>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
  options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "Enter your JWT token."
  });

  options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddHttpClient<IBibleService, BibleService>(client =>
{
  client.BaseAddress = new Uri("https://api.esv.org/v3/");
});

var app = builder.Build();

app.UseCors("Angular");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
