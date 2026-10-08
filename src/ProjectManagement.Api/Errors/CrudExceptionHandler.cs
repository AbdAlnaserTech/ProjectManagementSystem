using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ProjectManagement.Api.Errors;

// Only the safe error boundary needed for CRUD; full Phase 6 infrastructure is deferred.
public class CrudExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken token)
    {
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        var (status, detail) = exception switch
        {
            CrudException error => (error.StatusCode, error.Message),
            DbUpdateConcurrencyException => (409, "The resource changed during this request."),
            _ when sql?.Number is 2601 or 2627 => (409, "A unique value already exists."),
            _ when sql?.Number == 547 => (409, "The change conflicts with related data."),
            _ => (500, "An unexpected error occurred.")
        };
        context.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status, Title = ReasonPhrases.GetReasonPhrase(status), Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (!await problems.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem }))
            await context.Response.WriteAsJsonAsync(problem, token);
        return true;
    }
}
