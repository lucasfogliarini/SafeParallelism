using Microsoft.EntityFrameworkCore;

namespace SafeParallelism.Concurrency;

public static class CreateIssueEndpoint
{
    public static void MapCreateIssueEndpoint(WebApplication app)
    {
        app.MapPost("/issues", async (CreateIssueRequest request, HttpContext context, IssueDbContext dbContext) =>
        {
            try
            {
                var newIssue = new Issue
                {
                    Id = Guid.NewGuid(),
                    Title = request.Title,
                    Description = request.Description,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                dbContext.Issues.Add(newIssue);
                await dbContext.SaveChangesAsync(context.RequestAborted);

                return Results.Created($"/issues/{newIssue.Id}", newIssue);
            }
            catch (DbUpdateException ex) when (ex.InnerException != null && 
                (ex.InnerException.Message.Contains("UNIQUE constraint failed") || 
                 ex.InnerException.Message.Contains("duplicate key")))
            {
                return Results.Conflict($"An issue with title '{request.Title}' already exists.");
            }
        });
    }
}

public record CreateIssueRequest(string Title, string Description = "");