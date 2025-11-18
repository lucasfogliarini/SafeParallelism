using SafeParallelism.Idempotency;
using SafeParallelism.Concurrency;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
var connectionString = builder.Configuration.GetConnectionString("ConcurrencyDb");

builder.Services.AddDbContext<IssueDbContext>(options => 
    options.UseSqlServer(connectionString));

var app = builder.Build();

app.UseHttpsRedirection();

IdempotencyEndpoint.MapIdempotencyEndpoint(app);
CreateIssueEndpoint.MapCreateIssueEndpoint(app);
UpdateIssueEndpoint.MapUpdateIssueEndpoint(app);

app.Run();