using Microsoft.Extensions.Caching.Memory;

namespace SafeParallelism.Idempotency;

public static class IdempotencyEndpoint
{
    public static void MapIdempotencyEndpoint(WebApplication app)
    {
        app.MapPost("/idempotency", async (IdempotencyRequest request, HttpContext context, IMemoryCache cache) =>
        {
            var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
            if (string.IsNullOrEmpty(idempotencyKey))
                return Results.BadRequest("Header 'Idempotency-Key' é obrigatório");

            var cacheKey = $"idempotency_{idempotencyKey}";

            var result = await cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

                await Task.Delay(1000, context.RequestAborted);

                context.Response.Headers.Append("X-Cache", "MISS");
                context.Response.Headers.Append("X-Cache-Key", idempotencyKey);

                return new IdempotencyResult(
                    Id: idempotencyKey,
                    ProcessedValue: request.Value,
                    ProcessedAt: DateTime.UtcNow
                );
            });

            if (!context.Response.Headers.ContainsKey("X-Cache"))
            {
                context.Response.Headers.Append("X-Cache", "HIT");
                context.Response.Headers.Append("X-Cache-Key", idempotencyKey);
            }

            return Results.Ok(result);
        });
    }
}

public record IdempotencyRequest(int Value);
public record IdempotencyResult(string Id, int ProcessedValue, DateTime ProcessedAt);
