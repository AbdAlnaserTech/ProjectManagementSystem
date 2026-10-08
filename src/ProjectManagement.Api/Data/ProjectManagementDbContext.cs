using Microsoft.EntityFrameworkCore;
using ProjectManagement.Api.Models;

namespace ProjectManagement.Api.Data;

public class ProjectManagementDbContext(DbContextOptions<ProjectManagementDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProjectManagementDbContext).Assembly);
}
