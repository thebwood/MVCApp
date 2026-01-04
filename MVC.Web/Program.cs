using MVC.Web.Middleware;
using MVC.Web.Services;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Configure Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register the Address API Service
builder.Services.AddScoped<IAddressApiService, AddressApiService>();

// Configure HttpClient with Polly resilience policies
builder.Services.AddHttpClient("AddressApi")
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy())
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

// Also add the default HttpClient factory for backward compatibility
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Use custom exception handling middleware in production
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseHsts();
}
else
{
    // Use developer exception page in development
    app.UseDeveloperExceptionPage();
}

// Add request logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Retry policy: Retry 3 times with exponential backoff
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                var logger = context.GetLogger();
                logger?.LogWarning(
                    "Retry {RetryCount} after {Delay}s due to {Reason}",
                    retryCount,
                    timespan.TotalSeconds,
                    outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString());
            });
}

// Circuit breaker policy: Break circuit after 5 consecutive failures
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, duration, context) =>
            {
                var logger = context.GetLogger();
                logger?.LogWarning(
                    "Circuit breaker opened for {Duration}s due to {Reason}",
                    duration.TotalSeconds,
                    outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString());
            },
            onReset: context =>
            {
                var logger = context.GetLogger();
                logger?.LogInformation("Circuit breaker reset");
            });
}

// Extension method to get logger from Polly context
static class PollyContextExtensions
{
    private const string LoggerKey = "Logger";

    public static Context WithLogger(this Context context, ILogger logger)
    {
        context[LoggerKey] = logger;
        return context;
    }

    public static ILogger? GetLogger(this Context context)
    {
        return context.TryGetValue(LoggerKey, out var logger) ? logger as ILogger : null;
    }
}
