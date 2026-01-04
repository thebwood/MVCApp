using MVC.Web.Middleware;
using MVC.Web.Services;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Events;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .Enrich.WithProcessId()
    .Enrich.WithProcessName()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/mvc-web-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10485760) // 10MB
    .CreateLogger();

try
{
    Log.Information("Starting MVC.Web application");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog to the logging pipeline
    builder.Host.UseSerilog();

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

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
        options.GetLevel = (httpContext, elapsed, ex) => ex != null
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode > 499
                ? LogEventLevel.Error
                : httpContext.Response.StatusCode > 399
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
            diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
        };
    });

    // Add custom request logging middleware for additional tracking
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    Log.Information("MVC.Web application started successfully");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

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
                Log.Warning(
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
                Log.Warning(
                    "Circuit breaker opened for {Duration}s due to {Reason}",
                    duration.TotalSeconds,
                    outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString());
            },
            onReset: context =>
            {
                Log.Information("Circuit breaker reset");
            });
}
