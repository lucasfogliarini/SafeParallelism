using Xunit;
using System.Net;

namespace SafeParallelism.Concurrency;

public class IssueTests(SqlServerTestWebApplicationFactory factory) : IClassFixture<SqlServerTestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateIssue_WithSameTitle_ShouldPreventDuplicates()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        var title = $"Duplicate Test {Guid.NewGuid()}";
        var request = new CreateIssueRequest(title, "Test description");

        // Act - Criar a primeira issue
        var response1 = await _client.PostAsJsonAsync("/issues", request);
        // Tentar criar segunda issue com mesmo título
        var response2 = await _client.PostAsJsonAsync("/issues", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);

        var errorMessage = await response2.Content.ReadAsStringAsync();
        Assert.Contains($"An issue with title '{title}' already exists.", errorMessage);
    }

    [Fact]
    public async Task CreateIssue_WithDifferentTitles_ShouldBothSucceed()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        var request1 = new CreateIssueRequest($"Issue 1 {Guid.NewGuid()}", "Description 1");
        var request2 = new CreateIssueRequest($"Issue 2 {Guid.NewGuid()}", "Description 2");

        // Act
        var response1 = await _client.PostAsJsonAsync("/issues", request1);
        var response2 = await _client.PostAsJsonAsync("/issues", request2);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        var issue1 = await response1.Content.ReadFromJsonAsync<Issue>();
        var issue2 = await response2.Content.ReadFromJsonAsync<Issue>();

        Assert.NotNull(issue1);
        Assert.NotNull(issue2);
        Assert.NotEqual(issue1.Id, issue2.Id);
    }

    [Fact]
    public async Task UpdateIssue_WithValidData_ShouldSucceed()
    {
        // Arrange - Criar uma issue primeiro
        await factory.ResetDatabaseAsync();
        var createRequest = new CreateIssueRequest($"Original Title {Guid.NewGuid()}", "Original description");
        var createResponse = await _client.PostAsJsonAsync("/issues", createRequest);
        var createdIssue = await createResponse.Content.ReadFromJsonAsync<Issue>();
        Assert.NotNull(createdIssue);

        // Act - Atualizar a issue
        var updateRequest = new UpdateIssueRequest("Updated Title", "Updated description");
        var updateResponse = await _client.PutAsJsonAsync($"/issues/{createdIssue.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedIssue = await updateResponse.Content.ReadFromJsonAsync<Issue>();
        Assert.NotNull(updatedIssue);
        Assert.Equal("Updated Title", updatedIssue.Title);
        Assert.Equal("Updated description", updatedIssue.Description);
    }

    [Fact]
    public async Task UpdateIssue_WithConcurrentModification_ShouldReturnConflict()
    {
        // Arrange - Criar uma issue
        await factory.ResetDatabaseAsync();
        var createRequest = new CreateIssueRequest($"Concurrency Test {Guid.NewGuid()}", "Original description");
        var createResponse = await _client.PostAsJsonAsync("/issues", createRequest);
        var createdIssue = await createResponse.Content.ReadFromJsonAsync<Issue>();
        Assert.NotNull(createdIssue);

        var firstUpdate = new UpdateIssueRequest("First Update", "First description");
        var task1 = _client.PutAsJsonAsync($"/issues/{createdIssue.Id}", firstUpdate);
        var conflictingUpdate = new UpdateIssueRequest("Conflicting Update", "Conflicting description");
        var task2 = _client.PutAsJsonAsync($"/issues/{createdIssue.Id}", conflictingUpdate);

         var responses = await Task.WhenAll(task1, task2);

        // Assert
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.Conflict);
    }
}