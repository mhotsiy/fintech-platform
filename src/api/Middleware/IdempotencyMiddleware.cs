namespace FintechPlatform.Api.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IIdempotencyStore idempotencyStore)
    {
        // Only apply to POST requests
        if (context.Request.Method != HttpMethods.Post)
        {
            await _next(context);
            return;
        }

        // Check for Idempotency-Key header
        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyValues))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = idempotencyKeyValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _next(context);
            return;
        }

        _logger.LogInformation("Processing request with idempotency key: {IdempotencyKey}", idempotencyKey);

        // Check if we've seen this key before
        var cachedResponse = await idempotencyStore.GetResponseAsync(idempotencyKey);
        if (cachedResponse != null)
        {
            _logger.LogInformation("Returning cached response for idempotency key: {IdempotencyKey}", idempotencyKey);
            
            context.Response.StatusCode = cachedResponse.StatusCode;
            context.Response.ContentType = cachedResponse.ContentType;
            await context.Response.WriteAsync(cachedResponse.Body);
            return;
        }

        // Capture the response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            // Only cache successful responses (2xx status codes)
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(responseBody).ReadToEndAsync();
                
                await idempotencyStore.StoreResponseAsync(
                    idempotencyKey,
                    context.Response.StatusCode,
                    context.Response.ContentType ?? "application/json",
                    responseText);

                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            else
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}

public interface IIdempotencyStore
{
    Task<CachedResponse?> GetResponseAsync(string idempotencyKey);
    Task StoreResponseAsync(string idempotencyKey, int statusCode, string contentType, string body);
}

public class CachedResponse
{
    public int StatusCode { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
