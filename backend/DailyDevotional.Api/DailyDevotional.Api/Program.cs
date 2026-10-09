using DailyDevotional.Api.Data;
using DailyDevotional.Api.Models;
using DailyDevotional.Api.Services;
using DailyDevotional.Api.Services.IServices;
using DailyDevotional.Api.Services.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "5184";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var allowedOrigins = builder.Configuration["AllowedOrigins"]
   ?.Split(',', StringSplitOptions.RemoveEmptyEntries |
   StringSplitOptions.TrimEntries)
   ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
  options.AddPolicy("Angular", policy =>
  {
    policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod();
  });
});

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = string.IsNullOrWhiteSpace(databaseUrl)
  ? builder.Configuration.GetConnectionString("DefaultConnection")
  : BuildConnectionStringFromDatabaseUrl(databaseUrl);

builder.Services.AddDbContext<AppDbContext>(options =>
options.UseNpgsql(connectionString));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
  options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
  options.KnownNetworks.Clear();
  options.KnownProxies.Clear();
});

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
builder.Services.AddScoped<IAiCommentaryService, AiCommentaryService>();

// Add services to the container.

builder.Services.AddControllers();
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
var rateLimiting = builder.Configuration.GetSection("RateLimiting").Get<RateLimitingOptions>() ?? new RateLimitingOptions();
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IProviderRateLimiter, ProviderRateLimiter>();

// Per-client limit on the endpoints that return Bible text, so one visitor cannot use up
// the ESV allowance. Signed-in users are counted by account, everyone else by IP address.
builder.Services.AddRateLimiter(options =>
{
  options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

  options.AddPolicy("passages", httpContext =>
  {
    var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var partitionKey = userId != null
      ? $"user:{userId}"
      : $"ip:{httpContext.Connection.RemoteIpAddress}";

    return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
    {
      PermitLimit = rateLimiting.PassageRequestsPerMinute,
      Window = TimeSpan.FromMinutes(1),
      SegmentsPerWindow = 6,
      QueueLimit = 0
    });
  });

  options.OnRejected = async (context, cancellationToken) =>
  {
    // A sliding window does not report when a permit frees up, so fall back to its length.
    var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
      ? Math.Ceiling(retryAfter.TotalSeconds)
      : 60;

    context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
    await context.HttpContext.Response.WriteAsJsonAsync(
      new { errors = new[] { "Too many requests. Please wait a moment and try again." } },
      cancellationToken);
  };
});

builder.Services.AddHttpClient<EsvTranslationProvider>(client =>
{
  client.BaseAddress = new Uri("https://api.esv.org/v3/");
});
builder.Services.AddTransient<ITranslationProvider>(sp => sp.GetRequiredService<EsvTranslationProvider>());
builder.Services.AddScoped<IBibleTextService, BibleTextService>();
builder.Services.AddScoped<IBibleMetadataService, BibleMetadataService>();
builder.Services.AddHttpClient<TranslationImportService>(client =>
{
  // The KJV file is about 12 MB; allow for a slow connection.
  client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddHostedService<FullTranslationBootstrapService>();
builder.Services.AddHostedService<TranslationCacheCleanupService>();

var app = builder.Build();

using (var migrationScope = app.Services.CreateScope())
{
  var dbContext = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
  dbContext.Database.Migrate();

  await migrationScope.ServiceProvider.GetRequiredService<TranslationImportService>().SeedStructureAsync();

  // Usage: dotnet run -- import-translation <CODE> [path-to-bolls-json]
  if (args.Length >= 2 && args[0] == "import-translation")
  {
    var imported = await migrationScope.ServiceProvider
      .GetRequiredService<TranslationImportService>()
      .ImportFullTranslationAsync(args[1], args.Length > 2 ? args[2] : null);

    Console.WriteLine($"Imported {imported} verses of {args[1]}.");
    return;
  }
}

app.UseForwardedHeaders();

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
app.UseRateLimiter();

app.MapControllers();

app.Run();


static string BuildConnectionStringFromDatabaseUrl(string databaseUrl)
{
  var uri = new Uri(databaseUrl);
  var userInfo = uri.UserInfo.Split(':', 2);

  return new NpgsqlConnectionStringBuilder
  {
    Host = uri.Host,
    Port = uri.Port,
    Database = uri.AbsolutePath.TrimStart('/'),
    Username = Uri.UnescapeDataString(userInfo[0]),
    Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
    SslMode = SslMode.Require,
    TrustServerCertificate = true,
  }.ConnectionString;
}
