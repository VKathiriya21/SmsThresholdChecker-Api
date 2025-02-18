using Serilog;
using SmsThresholdChecker.Api.Interfaces;
using SmsThresholdChecker.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();
builder.Services.AddSingleton<ISmsCounter, SmsCounter>();
builder.Services.AddSingleton<ISmsRateLimiter>(provider =>
{
    int perNumberLimit = config.GetValue<int>("RateLimits:PerNumber");
    int accountLimit = config.GetValue<int>("RateLimits:Account");
    var cleanupInterval = TimeSpan.FromMinutes(config.GetValue<int>("RateLimits:CleanupIntervalMinutes"));
    var timeProvider = provider.GetRequiredService<ITimeProvider>();
    return new SmsRateLimiter(perNumberLimit, accountLimit, cleanupInterval, timeProvider);
});

builder.Services.AddCors(options =>
{
    var corsSettings = config.GetSection("Cors");
    var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>()
                         ?? corsSettings["AllowedOrigins"]?.Split(',')
                         ?? Array.Empty<string>();

    options.AddPolicy("AppCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateBootstrapLogger();

builder.Host.UseSerilog();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseSerilogRequestLogging();
app.UseCors("AppCorsPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();