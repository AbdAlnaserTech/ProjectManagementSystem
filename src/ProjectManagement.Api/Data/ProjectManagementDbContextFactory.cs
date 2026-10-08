using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProjectManagement.Api.Data;

public class ProjectManagementDbContextFactory : IDesignTimeDbContextFactory<ProjectManagementDbContext>
{
    public ProjectManagementDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Set ConnectionStrings__DefaultConnection securely before running EF tools.");
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>().UseSqlServer(connection).Options;
        return new ProjectManagementDbContext(options);
    }
}
