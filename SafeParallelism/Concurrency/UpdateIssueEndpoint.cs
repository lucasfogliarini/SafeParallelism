using Microsoft.EntityFrameworkCore;

namespace SafeParallelism.Concurrency;

public static class UpdateIssueEndpoint
{
    public static void MapUpdateIssueEndpoint(WebApplication app)
    {
        app.MapPut("/issues/{id:guid}", async (Guid id, UpdateIssueRequest request, HttpContext context, IssueDbContext dbContext) =>
        {
            
            try
            {
                var existingIssue = await dbContext.Issues.FirstOrDefaultAsync(i => i.Id == id, context.RequestAborted);
                if (existingIssue == null)
                    return Results.NotFound($"Issue with ID {id} not found.");

                existingIssue.Title = request.Title;
                existingIssue.Description = request.Description;
                existingIssue.UpdatedAt = DateTime.Now;

                await Task.Delay(100);

                await dbContext.SaveChangesAsync(context.RequestAborted);

                return Results.Ok(existingIssue);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Results.Conflict("Concurrency conflict - the issue was modified by another process. Please refresh and try again.");
            }
        });
    }
}

public record UpdateIssueRequest(string? Title, string? Description);