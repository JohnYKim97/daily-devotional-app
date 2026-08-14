using DailyDevotional.Api.Data;
using DailyDevotional.Api.Models;
using DailyDevotional.Api.Services;
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

var jwtKey =
    builder.Configuration["Authentication:Jwt:Key"];

builder.Services
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

        ValidIssuer =
              builder.Configuration["Authentication:Jwt:Issuer"],

        ValidAudience =
              builder.Configuration["Authentication:Jwt:Audience"],

        IssuerSigningKey =
              new SymmetricSecurityKey(
                  Encoding.UTF8.GetBytes(jwtKey!)
              )
      };
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddGoogle(options =>
    {
      options.ClientId =
          builder.Configuration["Authentication:Google:ClientId"]!;

      options.ClientSecret =
          builder.Configuration["Authentication:Google:ClientSecret"]!;

      options.SignInScheme =
          CookieAuthenticationDefaults.AuthenticationScheme;
    });

builder.Services.AddScoped<IJournalService, JournalService>();
builder.Services.AddScoped<IDailyReadingService, DailyReadingService>();
builder.Services.AddScoped<JwtService>();

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
