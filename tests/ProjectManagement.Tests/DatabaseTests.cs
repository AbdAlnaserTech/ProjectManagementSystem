using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using ProjectManagement.Api.Data;
using ProjectManagement.Api.Models;

namespace ProjectManagement.Tests;

// Each test owns a fresh SQL Server database, created by the real migration.
// Missing SQL configuration fails the suite; relational tests are never silently skipped.
public class DatabaseTests : IAsyncLifetime
{
    private readonly string _databaseName = "PmsTests_" + Guid.NewGuid().ToString("N");
    private string _connection = string.Empty;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("PMS_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("Set PMS_TEST_CONNECTION or run scripts/verify.sh with SQL configuration.");
        _connection = new SqlConnectionStringBuilder(configured) { InitialCatalog = _databaseName }.ConnectionString;
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrEmpty(_connection)) return;
        await using var db = CreateContext();
        // This connection points only to the unique database this test created.
        await db.Database.EnsureDeletedAsync();
    }

    private ProjectManagementDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ProjectManagementDbContext>().UseSqlServer(_connection).Options);

    private static Employee Employee(string email = "manager@example.test") => new() { FullName = "Test Employee", Email = email };
    private static Project Project(Employee manager) => new()
    {
        Name = "Database verification", Manager = manager, Status = ProjectStatus.Active,
        StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc)
    };
    private static ProjectTask ProjectTask(Project project, Employee employee) => new()
    {
        Title = "Test task", Project = project, AssignedEmployee = employee,
        DueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        Priority = TaskPriority.Critical, Status = ProjectTaskStatus.InProgress
    };

    private async Task<(int ProjectId, int ManagerId, int AssigneeId)> SeedGraphAsync()
    {
        await using var db = CreateContext();
        var manager = Employee();
        var assignee = Employee("assignee@example.test");
        var project = Project(manager);
        db.ProjectTasks.Add(ProjectTask(project, assignee));
        await db.SaveChangesAsync();
        return (project.Id, manager.Id, assignee.Id);
    }

    [Fact]
    public async Task ApplicationDependencyInjectionResolvesSqlServerContext()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:DefaultConnection", _connection));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectManagementDbContext>();
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", db.Database.ProviderName);
        Assert.Equal(_databaseName, db.Database.GetDbConnection().Database);
        Assert.Empty(await db.Employees.ToListAsync());
    }

    [Fact]
    public async Task MigrationIsAppliedAndRepeatable()
    {
        await using var db = CreateContext();
        Assert.Single(await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        await db.Database.MigrateAsync();
        Assert.Single(await db.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task RelationshipsAndEnumsRoundTrip()
    {
        var graph = await SeedGraphAsync();
        await using var db = CreateContext();
        var task = await db.ProjectTasks.Include(x => x.Project).ThenInclude(x => x.Manager)
            .Include(x => x.AssignedEmployee).SingleAsync();
        Assert.Equal(graph.ProjectId, task.ProjectId);
        Assert.Equal(graph.ManagerId, task.Project.Manager.Id);
        Assert.Equal(graph.AssigneeId, task.AssignedEmployee.Id);
        Assert.Equal(TaskPriority.Critical, task.Priority);
        Assert.Equal(ProjectTaskStatus.InProgress, task.Status);
        Assert.Equal(ProjectStatus.Active, task.Project.Status);
        Assert.Null(task.Description);
        Assert.Null(task.Project.Description);
    }

    [Theory]
    [InlineData("MANAGER@EXAMPLE.TEST")]
    [InlineData("manager@example.test ")]
    public async Task EmailUniquenessIsEnforcedBySqlServer(string duplicate)
    {
        await using var db = CreateContext();
        db.Employees.Add(Employee());
        await db.SaveChangesAsync();
        // Bypass EF to prove enforcement by the unique index/collation itself.
        var error = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO Employees (FullName, Email) VALUES (N'Duplicate', {duplicate})"));
        Assert.Equal(2601, error.Number);
        Assert.Equal(1, await db.Employees.CountAsync());
    }

    [Fact]
    public async Task IsActiveDefaultsToTrueAndExplicitFalseIsPreserved()
    {
        await using var db = CreateContext();
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Employees (FullName, Email) VALUES (N'Default', N'default@example.test')");
        var inactive = Employee("inactive@example.test");
        inactive.IsActive = false;
        db.Employees.Add(inactive);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.True((await db.Employees.SingleAsync(x => x.Email == "default@example.test")).IsActive);
        Assert.False((await db.Employees.SingleAsync(x => x.Email == "inactive@example.test")).IsActive);
    }

    [Theory]
    [InlineData("manager")]
    [InlineData("project")]
    [InlineData("assignee")]
    public async Task MissingForeignKeysAreRejected(string relationship)
    {
        await using var db = CreateContext();
        if (relationship == "manager")
        {
            var project = Project(Employee());
            project.Manager = null!;
            project.ManagerId = int.MaxValue;
            db.Projects.Add(project);
        }
        else
        {
            var graph = await SeedGraphAsync();
            db.ProjectTasks.Add(new ProjectTask
            {
                Title = "Broken relationship", DueDate = DateTime.UtcNow,
                ProjectId = relationship == "project" ? int.MaxValue : graph.ProjectId,
                AssignedEmployeeId = relationship == "assignee" ? int.MaxValue : graph.AssigneeId
            });
        }
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(547, Assert.IsType<SqlException>(error.InnerException).Number);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReferencedEmployeeDeletionIsRejectedAndGraphUnchanged(bool manager)
    {
        var graph = await SeedGraphAsync();
        await using var db = CreateContext();
        var id = manager ? graph.ManagerId : graph.AssigneeId;
        var error = await Assert.ThrowsAsync<SqlException>(() => db.Employees.Where(x => x.Id == id).ExecuteDeleteAsync());
        Assert.Equal(547, error.Number);
        Assert.Equal(2, await db.Employees.CountAsync());
        Assert.Equal(1, await db.Projects.CountAsync());
        Assert.Equal(1, await db.ProjectTasks.CountAsync());
    }

    [Fact]
    public async Task ProjectDeletionCascadesTasksInDatabaseAndPreservesEmployees()
    {
        var graph = await SeedGraphAsync();
        await using var db = CreateContext();
        // No dependents loaded: SQL Server, rather than client tracking, cascades.
        Assert.Equal(1, await db.Projects.Where(x => x.Id == graph.ProjectId).ExecuteDeleteAsync());
        Assert.Empty(await db.ProjectTasks.ToListAsync());
        Assert.Empty(await db.Projects.ToListAsync());
        Assert.Equal(2, await db.Employees.CountAsync());
    }

    [Fact]
    public async Task UnreferencedEmployeeAndIndividualTaskCanBeDeleted()
    {
        await SeedGraphAsync();
        await using var db = CreateContext();
        Assert.Equal(1, await db.ProjectTasks.ExecuteDeleteAsync());
        var assignee = await db.Employees.SingleAsync(x => x.Email == "assignee@example.test");
        Assert.Equal(1, await db.Employees.Where(x => x.Id == assignee.Id).ExecuteDeleteAsync());
        Assert.Equal(1, await db.Projects.CountAsync());
        Assert.Equal(1, await db.Employees.CountAsync());
    }

    [Theory]
    [InlineData("project")]
    [InlineData("task-status")]
    [InlineData("priority")]
    public async Task UndefinedEnumValuesAreRejected(string field)
    {
        var graph = await SeedGraphAsync();
        await using var db = CreateContext();
        if (field == "project") (await db.Projects.SingleAsync()).Status = (ProjectStatus)99;
        else
        {
            var task = await db.ProjectTasks.SingleAsync();
            if (field == "priority") task.Priority = (TaskPriority)99;
            else task.Status = (ProjectTaskStatus)99;
        }
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(547, Assert.IsType<SqlException>(error.InnerException).Number);
        await using var verify = CreateContext();
        Assert.Equal(ProjectStatus.Active, (await verify.Projects.SingleAsync()).Status);
        Assert.Equal(TaskPriority.Critical, (await verify.ProjectTasks.SingleAsync()).Priority);
    }

    [Fact]
    public async Task ReversedProjectDatesAreRejected()
    {
        await using var db = CreateContext();
        var project = Project(Employee());
        project.EndDate = project.StartDate.AddDays(-1);
        db.Projects.Add(project);
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(547, Assert.IsType<SqlException>(error.InnerException).Number);
        await using var verify = CreateContext();
        Assert.Empty(await verify.Projects.ToListAsync());
        Assert.Empty(await verify.Employees.ToListAsync()); // SaveChanges rolled back the dependent insert too.
    }

    [Theory]
    [InlineData("name")]
    [InlineData("email")]
    [InlineData("project")]
    [InlineData("task")]
    public async Task BlankRequiredTextIsRejected(string field)
    {
        await using var db = CreateContext();
        var employee = Employee();
        if (field is "name" or "email")
        {
            if (field == "name") employee.FullName = "   ";
            else employee.Email = "   ";
            db.Employees.Add(employee);
        }
        else
        {
            var project = Project(employee);
            if (field == "project") { project.Name = "   "; db.Projects.Add(project); }
            else { var task = ProjectTask(project, employee); task.Title = "   "; db.ProjectTasks.Add(task); }
        }
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(547, Assert.IsType<SqlException>(error.InnerException).Number);
    }

    [Fact]
    public async Task NullRequiredEmailAndOverlongNameAreRejected()
    {
        await using var db = CreateContext();
        var employee = Employee();
        employee.Email = null!;
        db.Employees.Add(employee);
        var nullError = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(515, Assert.IsType<SqlException>(nullError.InnerException).Number);
        db.ChangeTracker.Clear();
        employee = Employee();
        employee.FullName = new string('x', 201);
        db.Employees.Add(employee);
        var lengthError = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains(Assert.IsType<SqlException>(lengthError.InnerException).Number, new[] { 2628, 8152 });
    }
}
