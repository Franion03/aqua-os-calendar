using System.Text;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using AquaOs.Calendar.Data.DynamoDb;
using AquaOs.Calendar.Repositories;
using AquaOs.Calendar.Services.GoogleCalendar;
using AquaOs.Calendar.Services.Ics;
using AquaOs.Calendar.Services.Notifications;
using AquaOs.Calendar.Services.Orchestrator;
using AquaOs.Calendar.Services.Pollers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ── DynamoDB ──
builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
{
    AmazonDynamoDBConfig config = new AmazonDynamoDBConfig
    {
        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(
            builder.Configuration["AWS:Region"] ?? "eu-west-1")
    };
    return new AmazonDynamoDBClient(config);
});
builder.Services.AddSingleton<DynamoDBContext>(sp =>
    new DynamoDBContext(sp.GetRequiredService<IAmazonDynamoDB>()));
builder.Services.AddSingleton<IDynamoDbContext>(sp =>
    new DynamoDbContextAdapter(sp.GetRequiredService<DynamoDBContext>()));

// ── JWT ──
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        IConfigurationSection jwt = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            // ReSharper disable once NullableWarningSuppressionIsUsed
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });

builder.Services.AddAuthorization();

// ── Repositories ──
builder.Services.AddScoped<IPollingConfigRepository, DynamoDbPollingConfigRepository>();
builder.Services.AddScoped<ISeriesRepository>(sp =>
    new DynamoDbSeriesRepository(
        sp.GetRequiredService<IDynamoDbContext>(),
        sp.GetRequiredService<IPollingConfigRepository>()));
builder.Services.AddScoped<IManualEventRepository, DynamoDbManualEventRepository>();

// ── Services ──
builder.Services.AddSingleton<IIcsService, IcsService>();
builder.Services.AddTransient<StubPoller>();
builder.Services.AddHttpClient<Poller1>();
builder.Services.AddTransient<ManualEventPoller>();
builder.Services.AddHttpClient<WaterPoloLeaguePoller>();
builder.Services.AddTransient<IPollerFactory, PollerFactory>();

// ── Notifications ──
string? crewUrl = builder.Configuration["CREW_SERVICE_URL"];
if (!string.IsNullOrWhiteSpace(crewUrl))
{
    builder.Services.AddHttpClient<INotificationService, WebhookNotificationService>();
}
else
{
    builder.Services.AddSingleton<INotificationService, LogNotificationService>();
}

// ── Google Calendar ──
builder.Services.AddSingleton<IGoogleCalendarService, GoogleCalendarService>();

// ── Orchestrator ──
builder.Services.AddHostedService<OrchestratorService>();

builder.Services.AddControllers();

WebApplication app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
