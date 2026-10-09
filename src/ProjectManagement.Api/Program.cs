using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Api.Errors;
using ProjectManagement.Api.Interfaces;
using ProjectManagement.Api.Serialization;
using ProjectManagement.Api.Services;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Api.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.Converters.Add(new ExplicitOffsetDateTimeConverter());
}).ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = 400, Title = "Invalid request data.", Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        return new BadRequestObjectResult(problem);
    };
});
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CrudExceptionHandler>();
builder.Services.AddDbContext<ProjectManagementDbContext>(options =>
{
    var connection = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException("Configure ConnectionStrings__DefaultConnection securely to use persistence.");
    options.UseSqlServer(connection);
});
builder.Services.AddHealthChecks();
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapControllers();
// Process liveness only; migration/readiness is verified separately.
app.MapHealthChecks("/health/live");
app.Run();
public partial class Program { }
