using FintechPlatform.Api.Middleware;
using Microsoft.Extensions.Caching.Memory;

namespace FintechPlatform.Api.Services;

public class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(24);

    public InMemoryIdempotencyStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<CachedResponse?> GetResponseAsync(string idempotencyKey)
    {
        var key = $"idempotency:{idempotencyKey}";
        _cache.TryGetValue(key, out CachedResponse? response);
        return Task.FromResult(response);
    }

    public Task StoreResponseAsync(string idempotencyKey, int statusCode, string contentType, string body)
    {
        var key = $"idempotency:{idempotencyKey}";
        var response = new CachedResponse
        {
            StatusCode = statusCode,
            ContentType = contentType,
            Body = body
        };

        _cache.Set(key, response, _cacheExpiration);
        return Task.CompletedTask;
    }
}
