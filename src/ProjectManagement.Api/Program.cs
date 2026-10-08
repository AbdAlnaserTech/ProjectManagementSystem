var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
var app = builder.Build();
app.MapControllers();
// Process liveness only. Database readiness is added when persistence exists.
app.MapHealthChecks("/health/live");
app.Run();
public partial class Program { }
