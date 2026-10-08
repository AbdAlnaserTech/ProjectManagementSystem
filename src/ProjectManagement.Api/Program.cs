using Microsoft.EntityFrameworkCore;
using ProjectManagement.Api.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<ProjectManagementDbContext>(options =>
{
    var connection = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException("Configure ConnectionStrings__DefaultConnection securely to use persistence.");
    options.UseSqlServer(connection);
});
builder.Services.AddHealthChecks();
var app = builder.Build();
app.MapControllers();
// Process liveness only; migration/readiness is verified separately.
app.MapHealthChecks("/health/live");
app.Run();
public partial class Program { }
