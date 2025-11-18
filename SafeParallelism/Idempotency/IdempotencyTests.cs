using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SafeParallelism.Idempotency;

public class IdempotencyTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task TwoRequestsWithSameKey_ShouldReturnCachedResult()
    {
        // Arrange
        var request1 = new IdempotencyRequest(10);
        var request2 = new IdempotencyRequest(999);
        var idempotencyKey = Guid.NewGuid().ToString();

        using var client1 = factory.CreateClient();
        using var client2 = factory.CreateClient();

        client1.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);
        client2.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        // Act - Run both requests
        var result1 = await MakeIdempotencyRequest(client1, request1);
        var result2 = await MakeIdempotencyRequest(client2, request2);

        // Assert
        Assert.NotNull(result1.Result);
        Assert.NotNull(result2.Result);
        Assert.Equal(result1.Result.Id, result2.Result.Id);
        Assert.Equal(result1.Result.ProcessedValue, result2.Result.ProcessedValue);
        Assert.Equal(result1.Result.ProcessedAt, result2.Result.ProcessedAt);

        AssertCacheHeaders(result1.Response, "MISS", idempotencyKey);
        AssertCacheHeaders(result2.Response, "HIT", idempotencyKey);
    }

    [Fact]
    public async Task TwoRequestsWithDifferentKeys_ShouldProcessBoth()
    {
        // Arrange
        var request1 = new IdempotencyRequest(20);
        var request2 = new IdempotencyRequest(30);
        var idempotencyKey1 = Guid.NewGuid().ToString();
        var idempotencyKey2 = Guid.NewGuid().ToString();

        // Act - First request
        _client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey1);
        var result1 = await MakeIdempotencyRequest(_client, request1);
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");

        // Act - Second request with different key
        _client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey2);
        var result2 = await MakeIdempotencyRequest(_client, request2);
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");

        // Assert
        Assert.NotNull(result1.Result);
        Assert.NotNull(result2.Result);
        Assert.NotEqual(result1.Result.Id, result2.Result.Id);
        Assert.Equal(20, result1.Result.ProcessedValue);
        Assert.Equal(30, result2.Result.ProcessedValue);
        Assert.NotEqual(result1.Result.ProcessedAt, result2.Result.ProcessedAt);
        AssertCacheHeaders(result1.Response, "MISS", idempotencyKey1);
        AssertCacheHeaders(result2.Response, "MISS", idempotencyKey2);
    }

    [Fact]
    public async Task RequestWithoutIdempotencyKey_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new IdempotencyRequest(40);

        // Act - Request without idempotency header
        var response = await _client.PostAsJsonAsync("/idempotency", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Header 'Idempotency-Key' é obrigatório", content);
    }

    private static async Task<(HttpResponseMessage Response, IdempotencyResult? Result)> MakeIdempotencyRequest(HttpClient client, IdempotencyRequest request)
    {
        var response = await client.PostAsJsonAsync("/idempotency", request);
        var result = await response.Content.ReadFromJsonAsync<IdempotencyResult>();
        return (response, result);
    }

    private static void AssertCacheHeaders(HttpResponseMessage response, string expectedCacheStatus, string expectedCacheKey)
    {
        Assert.True(response.Headers.Contains("X-Cache"));
        Assert.Equal(expectedCacheStatus, response.Headers.GetValues("X-Cache").First());
        Assert.True(response.Headers.Contains("X-Cache-Key"));
        Assert.Equal(expectedCacheKey, response.Headers.GetValues("X-Cache-Key").First());
    }
}