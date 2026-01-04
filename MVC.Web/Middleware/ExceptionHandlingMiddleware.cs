using System.Diagnostics;

namespace MVC.Web.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred. TraceId: {TraceId}", Activity.Current?.Id ?? context.TraceIdentifier);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "text/html";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            var html = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>Error - MVC.Web</title>
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' />
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css'>
    <style>
        body {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
        }}
        .error-container {{
            max-width: 600px;
            padding: 2rem;
        }}
        .error-card {{
            background: white;
            border-radius: 1rem;
            padding: 3rem;
            box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
        }}
        .error-icon {{
            font-size: 4rem;
            color: #dc3545;
        }}
        .error-title {{
            font-size: 2rem;
            font-weight: 700;
            color: #212529;
            margin: 1rem 0;
        }}
        .error-message {{
            color: #6c757d;
            margin-bottom: 1.5rem;
        }}
        .trace-id {{
            background-color: #f8f9fa;
            padding: 1rem;
            border-radius: 0.5rem;
            font-family: 'Courier New', monospace;
            font-size: 0.875rem;
            color: #495057;
            margin-bottom: 1.5rem;
        }}
        .btn-home {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            border: none;
            color: white;
            padding: 0.75rem 2rem;
            border-radius: 0.5rem;
            font-weight: 600;
            text-decoration: none;
            display: inline-flex;
            align-items: center;
            gap: 0.5rem;
            transition: all 0.2s ease;
        }}
        .btn-home:hover {{
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
            color: white;
        }}
    </style>
</head>
<body>
    <div class='error-container'>
        <div class='error-card text-center'>
            <i class='bi bi-exclamation-triangle-fill error-icon'></i>
            <h1 class='error-title'>Oops! Something went wrong</h1>
            <p class='error-message'>
                We're sorry, but an unexpected error occurred while processing your request.
                Our team has been notified and we're working to fix the issue.
            </p>
            <div class='trace-id'>
                <strong>Error ID:</strong> {traceId}
            </div>
            <a href='/' class='btn-home'>
                <i class='bi bi-house-door-fill'></i>
                Return to Home
            </a>
        </div>
    </div>
</body>
</html>";

            await context.Response.WriteAsync(html);
        }
    }
}
