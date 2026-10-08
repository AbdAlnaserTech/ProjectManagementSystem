using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Api.Data;

namespace ProjectManagement.Tests;

// Every case exercises HTTP against the application backed by its own migrated SQL Server database.
public class CrudApiTests : IAsyncLifetime
{
    private readonly string _databaseName = "PmsTests_" + Guid.NewGuid().ToString("N");
    private string _connection = string.Empty;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient _client = null!;
    private readonly QueryRecorder _queries = new();

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("PMS_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("Real SQL Server is required; run scripts/verify.sh with database configuration.");
        _connection = new SqlConnectionStringBuilder(configured) { InitialCatalog = _databaseName }.ConnectionString;
        await using var db = Database();
        await db.Database.MigrateAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:DefaultConnection", _connection)
                .ConfigureServices(services => services.AddDbContext<ProjectManagementDbContext>(options => options.AddInterceptors(_queries))));
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        if (!string.IsNullOrEmpty(_connection))
        {
            await using var db = Database();
            await db.Database.EnsureDeletedAsync();
        }
    }

    private ProjectManagementDbContext Database() => new(
        new DbContextOptionsBuilder<ProjectManagementDbContext>().UseSqlServer(_connection).Options);

    private static int Id(JsonElement json) => json.GetProperty("id").GetInt32();
    private static async Task<JsonElement> Json(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>();

    private async Task<JsonElement> Employee(string? email = null, string name = "Test Employee", bool active = true)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/employees", new
        { fullName = name, email = email ?? Guid.NewGuid().ToString("N") + "@example.test", isActive = active });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await Json(response);
    }

    private static Dictionary<string, object?> ProjectBody(int manager, string name = "Test Project", string status = "Planning") => new()
    {
        ["name"] = name, ["description"] = "Project details", ["startDate"] = "2026-01-01T00:00:00Z",
        ["endDate"] = "2026-12-31T00:00:00Z", ["status"] = status, ["managerId"] = manager
    };

    private async Task<JsonElement> Project(int manager, string name = "Test Project", string status = "Planning")
    {
        var response = await _client.PostAsJsonAsync("/api/v1/projects", ProjectBody(manager, name, status));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await Json(response);
    }

    private static Dictionary<string, object?> TaskBody(int project, int employee, string priority = "Medium", string status = "Pending") => new()
    {
        ["title"] = "Test Task", ["description"] = null, ["priority"] = priority, ["status"] = status,
        ["dueDate"] = "2026-06-01T00:00:00Z", ["projectId"] = project, ["assignedEmployeeId"] = employee
    };

    private async Task<JsonElement> TaskItem(int project, int employee, string priority = "Medium", string status = "Pending")
    {
        var response = await _client.PostAsJsonAsync("/api/v1/tasks", TaskBody(project, employee, priority, status));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await Json(response);
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = await Json(response);
        Assert.Equal((int)status, json.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("title").GetString()));
        Assert.True(json.TryGetProperty("traceId", out _));
        var text = await response.Content.ReadAsStringAsync();
        foreach (var sensitive in new[] { "SqlException", "SELECT ", "INSERT ", "ConnectionStrings", "StackTrace", "Microsoft.EntityFrameworkCore" })
            Assert.DoesNotContain(sensitive, text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmployeeCrudNormalizesEmailsAndReturnsDtoOnly()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/employees", new { fullName = "  Alice  ", email = "  ALICE@Example.Test  " });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var employee = await Json(response);
        var id = Id(employee);
        Assert.Equal($"/api/v1/employees/{id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal(new[] { "id", "fullName", "email", "isActive" }, employee.EnumerateObject().Select(x => x.Name));
        Assert.Equal("Alice", employee.GetProperty("fullName").GetString());
        Assert.Equal("alice@example.test", employee.GetProperty("email").GetString());
        Assert.True(employee.GetProperty("isActive").GetBoolean());
        Assert.Equal("alice@example.test", (await Json(await _client.GetAsync($"/api/v1/employees/{id}"))).GetProperty("email").GetString());

        response = await _client.PutAsJsonAsync($"/api/v1/employees/{id}", new { fullName = "Alice Updated", email = " alice@example.test ", isActive = false });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await Json(response)).GetProperty("isActive").GetBoolean());
        response = await _client.PutAsJsonAsync($"/api/v1/employees/{id}", new { fullName = "Alice Updated", email = "alice@example.test", isActive = true });
        Assert.True((await Json(response)).GetProperty("isActive").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/v1/employees/{id}")).StatusCode);
        await AssertProblem(await _client.GetAsync($"/api/v1/employees/{id}"), HttpStatusCode.NotFound);
        await using var db = Database();
        Assert.Empty(await db.Employees.ToListAsync());
    }

    [Fact]
    public async Task DuplicateEmployeeCreatesAndUpdatesReturnConflictWithoutChangingData()
    {
        var first = await Employee("first@example.test");
        var second = await Employee("second@example.test");
        await AssertProblem(await _client.PostAsJsonAsync("/api/v1/employees", new { fullName = "Duplicate", email = " FIRST@EXAMPLE.TEST " }), HttpStatusCode.Conflict);
        await AssertProblem(await _client.PutAsJsonAsync($"/api/v1/employees/{Id(second)}", new { fullName = "Changed", email = "FIRST@example.test", isActive = false }), HttpStatusCode.Conflict);
        await using var db = Database();
        Assert.Equal(2, await db.Employees.CountAsync());
        var unchanged = await db.Employees.SingleAsync(e => e.Id == Id(second));
        Assert.Equal("second@example.test", unchanged.Email);
        Assert.True(unchanged.IsActive);
        Assert.Equal("Test Employee", unchanged.FullName);
        Assert.True(Id(first) < Id(second));
    }

    [Fact]
    public async Task ConcurrentDuplicateEmployeeRequestsLeaveOneRow()
    {
        var body = new { fullName = "Duplicate race", email = "race@example.test" };
        var responses = await Task.WhenAll(_client.PostAsJsonAsync("/api/v1/employees", body), _client.PostAsJsonAsync("/api/v1/employees", body));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        await AssertProblem(Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict), HttpStatusCode.Conflict);
        await using var db = Database();
        Assert.Equal(1, await db.Employees.CountAsync());
    }

    [Fact]
    public async Task EmployeeSearchFiltersAndPaginationAreStable()
    {
        var first = await Employee("alpha@example.test", "First Marker");
        var second = await Employee("beta@example.test", "Second Marker");
        await Employee("marker@example.test", "Inactive", false);
        await Employee("other@example.test", "Other");
        var page = await Json(await _client.GetAsync("/api/v1/employees?search=marker&isActive=true&page=2&pageSize=1"));
        Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, page.GetProperty("totalPages").GetInt32());
        Assert.Equal(2, page.GetProperty("page").GetInt32());
        Assert.Equal(1, page.GetProperty("pageSize").GetInt32());
        Assert.Equal(Id(second), Id(page.GetProperty("items")[0]));
        var initial = await Json(await _client.GetAsync("/api/v1/employees?search=marker&isActive=true&pageSize=1"));
        Assert.Equal(Id(first), Id(initial.GetProperty("items")[0]));
        var emailSearch = await Json(await _client.GetAsync("/api/v1/employees?search=MARKER@EXAMPLE.TEST&isActive=false"));
        Assert.Equal(1, emailSearch.GetProperty("totalCount").GetInt32());
        var pastEnd = await Json(await _client.GetAsync("/api/v1/employees?page=100&pageSize=1"));
        Assert.Equal(4, pastEnd.GetProperty("totalCount").GetInt32());
        Assert.Empty(pastEnd.GetProperty("items").EnumerateArray());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReferencedEmployeeDeletionReturnsConflict(bool managerReference)
    {
        var manager = await Employee();
        var assignee = await Employee();
        var project = await Project(Id(manager));
        await TaskItem(Id(project), Id(assignee));
        var target = managerReference ? Id(manager) : Id(assignee);
        await AssertProblem(await _client.DeleteAsync($"/api/v1/employees/{target}"), HttpStatusCode.Conflict);
        await using var db = Database();
        Assert.Equal(2, await db.Employees.CountAsync());
        Assert.Equal(1, await db.Projects.CountAsync());
        Assert.Equal(1, await db.ProjectTasks.CountAsync());
    }

    [Fact]
    public async Task ProjectCrudNormalizesOffsetsAndCascadesDeletion()
    {
        var manager = await Employee();
        var body = ProjectBody(Id(manager));
        body["startDate"] = "2026-01-01T03:00:00+03:00";
        var response = await _client.PostAsJsonAsync("/api/v1/projects", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var project = await Json(response);
        var id = Id(project);
        Assert.Equal($"/api/v1/projects/{id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal("2026-01-01T00:00:00.0000000Z", project.GetProperty("startDate").GetString());
        Assert.Equal("Planning", project.GetProperty("status").GetString());
        Assert.Equal(Id(manager), Id(project.GetProperty("manager")));
        Assert.False(project.TryGetProperty("tasks", out _));
        var task = await TaskItem(id, Id(manager));
        body["name"] = "Updated Project";
        body["status"] = "Completed";
        response = await _client.PutAsJsonAsync($"/api/v1/projects/{id}", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Updated Project", (await Json(response)).GetProperty("name").GetString());
        Assert.Equal("Pending", (await Json(await _client.GetAsync($"/api/v1/tasks/{Id(task)}"))).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/v1/projects/{id}")).StatusCode);
        await AssertProblem(await _client.GetAsync($"/api/v1/projects/{id}"), HttpStatusCode.NotFound);
        await AssertProblem(await _client.GetAsync($"/api/v1/tasks/{Id(task)}"), HttpStatusCode.NotFound);
        await using var db = Database();
        Assert.Empty(await db.ProjectTasks.ToListAsync());
        Assert.Equal(1, await db.Employees.CountAsync());
    }

    [Fact]
    public async Task ProjectSearchStatusAndManagerFiltersCombineWithPagination()
    {
        var manager = await Employee();
        var other = await Employee();
        var first = await Project(Id(manager), "Release One", "Active");
        var secondBody = ProjectBody(Id(manager), "Second", "Active");
        secondBody["description"] = "Release from description";
        var secondResponse = await _client.PostAsJsonAsync("/api/v1/projects", secondBody);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var second = await Json(secondResponse);
        await Project(Id(manager), "Release Planning");
        await Project(Id(other), "Release Other", "Active");
        var page = await Json(await _client.GetAsync($"/api/v1/projects?search=Release&status=Active&managerId={Id(manager)}&page=2&pageSize=1"));
        Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, page.GetProperty("totalPages").GetInt32());
        Assert.Equal(Id(second), Id(page.GetProperty("items")[0]));
        Assert.True(Id(first) < Id(second));
    }

    [Fact]
    public async Task NewManagerAssignmentsRequireActiveEmployeeButExistingInactiveManagerCanBeRetained()
    {
        var active = await Employee();
        var inactive = await Employee(active: false);
        await AssertProblem(await _client.PostAsJsonAsync("/api/v1/projects", ProjectBody(Id(inactive))), HttpStatusCode.BadRequest);
        var project = await Project(Id(active));
        await AssertProblem(await _client.PutAsJsonAsync($"/api/v1/projects/{Id(project)}", ProjectBody(Id(inactive))), HttpStatusCode.BadRequest);
        var deactivation = await _client.PutAsJsonAsync($"/api/v1/employees/{Id(active)}", new
        { fullName = "Retired Manager", email = active.GetProperty("email").GetString(), isActive = false });
        Assert.Equal(HttpStatusCode.OK, deactivation.StatusCode);
        var retain = await _client.PutAsJsonAsync($"/api/v1/projects/{Id(project)}", ProjectBody(Id(active), "Retained"));
        Assert.Equal(HttpStatusCode.OK, retain.StatusCode);
        Assert.False((await Json(retain)).GetProperty("manager").GetProperty("isActive").GetBoolean());
        await using var db = Database();
        Assert.Equal(1, await db.Projects.CountAsync());
        Assert.Equal(Id(active), (await db.Projects.SingleAsync()).ManagerId);
    }

    [Fact]
    public async Task TaskCrudReturnsRelatedDtosAndUtc()
    {
        var manager = await Employee();
        var assignee = await Employee();
        var project = await Project(Id(manager));
        var body = TaskBody(Id(project), Id(assignee));
        body["dueDate"] = "2026-06-01T03:30:00+03:00";
        var response = await _client.PostAsJsonAsync("/api/v1/tasks", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await Json(response);
        var id = Id(task);
        Assert.Equal($"/api/v1/tasks/{id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal(Id(project), Id(task.GetProperty("project")));
        Assert.Equal(Id(assignee), Id(task.GetProperty("assignedEmployee")));
        Assert.Equal(new[] { "id", "name", "status" }, task.GetProperty("project").EnumerateObject().Select(x => x.Name));
        Assert.Equal("2026-06-01T00:30:00.0000000Z", task.GetProperty("dueDate").GetString());
        body["title"] = "Updated Task";
        body["status"] = "InProgress";
        body["priority"] = "Critical";
        response = await _client.PutAsJsonAsync($"/api/v1/tasks/{id}", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Critical", (await Json(response)).GetProperty("priority").GetString());
        var retrieved = await Json(await _client.GetAsync($"/api/v1/tasks/{id}"));
        Assert.Equal("Updated Task", retrieved.GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/v1/tasks/{id}")).StatusCode);
        await AssertProblem(await _client.GetAsync($"/api/v1/tasks/{id}"), HttpStatusCode.NotFound);
        await using var db = Database();
        Assert.Empty(await db.ProjectTasks.ToListAsync());
        Assert.Equal(1, await db.Projects.CountAsync());
        Assert.Equal(2, await db.Employees.CountAsync());
    }

    [Fact]
    public async Task TaskFiltersCombineAndPaginateInIdOrder()
    {
        var employee = await Employee();
        var other = await Employee();
        var project = await Project(Id(employee));
        var otherProject = await Project(Id(employee));
        var first = await TaskItem(Id(project), Id(employee), "High", "InProgress");
        var second = await TaskItem(Id(project), Id(employee), "High", "InProgress");
        await TaskItem(Id(project), Id(other), "High", "InProgress");
        await TaskItem(Id(project), Id(employee), "Low", "InProgress");
        await TaskItem(Id(project), Id(employee), "High", "Pending");
        await TaskItem(Id(otherProject), Id(employee), "High", "InProgress");
        var filter = $"/api/v1/tasks?projectId={Id(project)}&assignedEmployeeId={Id(employee)}&status=InProgress&priority=High&pageSize=1";
        var pageOne = await Json(await _client.GetAsync(filter));
        var pageTwo = await Json(await _client.GetAsync(filter + "&page=2"));
        Assert.Equal(2, pageTwo.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, pageTwo.GetProperty("totalPages").GetInt32());
        Assert.Equal(Id(first), Id(pageOne.GetProperty("items")[0]));
        Assert.Equal(Id(second), Id(pageTwo.GetProperty("items")[0]));
        var statusOnly = await Json(await _client.GetAsync("/api/v1/tasks?status=Pending"));
        Assert.Equal(1, statusOnly.GetProperty("totalCount").GetInt32());
        var priorityOnly = await Json(await _client.GetAsync("/api/v1/tasks?priority=Low"));
        Assert.Equal(1, priorityOnly.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task NewAssigneeMustBeActiveButExistingInactiveAssigneeCanBeRetained()
    {
        var manager = await Employee();
        var employee = await Employee();
        var inactive = await Employee(active: false);
        var project = await Project(Id(manager));
        await AssertProblem(await _client.PostAsJsonAsync("/api/v1/tasks", TaskBody(Id(project), Id(inactive))), HttpStatusCode.BadRequest);
        var task = await TaskItem(Id(project), Id(employee));
        await AssertProblem(await _client.PutAsJsonAsync($"/api/v1/tasks/{Id(task)}", TaskBody(Id(project), Id(inactive))), HttpStatusCode.BadRequest);
        await _client.PutAsJsonAsync($"/api/v1/employees/{Id(employee)}", new
        { fullName = "Inactive Assignee", email = employee.GetProperty("email").GetString(), isActive = false });
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync($"/api/v1/tasks/{Id(task)}", TaskBody(Id(project), Id(employee)))).StatusCode);
        await using var db = Database();
        Assert.Equal(Id(employee), (await db.ProjectTasks.SingleAsync()).AssignedEmployeeId);
    }

    [Theory]
    [InlineData("employees")]
    [InlineData("projects")]
    [InlineData("tasks")]
    public async Task EmptyCollectionsHaveConsistentPagination(string resource)
    {
        var response = await _client.GetAsync($"/api/v1/{resource}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await Json(response);
        Assert.Empty(page.GetProperty("items").EnumerateArray());
        Assert.Equal(1, page.GetProperty("page").GetInt32());
        Assert.Equal(20, page.GetProperty("pageSize").GetInt32());
        Assert.Equal(0, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(0, page.GetProperty("totalPages").GetInt32());
    }

    [Theory]
    [InlineData("employees")]
    [InlineData("projects")]
    [InlineData("tasks")]
    public async Task MissingResourcesReturn404ForAllMethods(string resource)
    {
        await AssertProblem(await _client.GetAsync($"/api/v1/{resource}/999"), HttpStatusCode.NotFound);
        object body = resource switch
        {
            "employees" => new { fullName = "Missing", email = "missing@example.test" },
            "projects" => ProjectBody(1),
            _ => TaskBody(1, 1)
        };
        await AssertProblem(await _client.PutAsJsonAsync($"/api/v1/{resource}/999", body), HttpStatusCode.NotFound);
        await AssertProblem(await _client.DeleteAsync($"/api/v1/{resource}/999"), HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("employees", "page=0")]
    [InlineData("employees", "pageSize=0")]
    [InlineData("employees", "pageSize=101")]
    [InlineData("employees", "page=2147483647&pageSize=100")]
    [InlineData("employees", "isActive=invalid")]
    [InlineData("projects", "page=-1")]
    [InlineData("projects", "status=99")]
    [InlineData("projects", "status=Unknown")]
    [InlineData("projects", "managerId=0")]
    [InlineData("tasks", "pageSize=101")]
    [InlineData("tasks", "priority=99")]
    [InlineData("tasks", "status=99")]
    [InlineData("tasks", "assignedEmployeeId=-1")]
    [InlineData("tasks", "projectId=0")]
    public async Task InvalidQueryInputsReturn400(string resource, string query)
        => await AssertProblem(await _client.GetAsync($"/api/v1/{resource}?{query}"), HttpStatusCode.BadRequest);

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"fullName\":\"   \",\"email\":\"valid@example.test\"}")]
    [InlineData("{\"fullName\":\"Name\",\"email\":\"not-an-email\"}")]
    [InlineData("{\"fullName\":\"Name\",\"email\":null}")]
    public async Task InvalidEmployeesReturn400WithoutPersisting(string body)
    {
        await AssertProblem(await _client.PostAsync("/api/v1/employees", new StringContent(body, Encoding.UTF8, "application/json")), HttpStatusCode.BadRequest);
        await using var db = Database();
        Assert.Empty(await db.Employees.ToListAsync());
    }

    [Theory]
    [InlineData("managerId", 999)]
    [InlineData("status", 99)]
    [InlineData("status", "Undefined")]
    [InlineData("startDate", "2026-01-01T00:00:00")]
    [InlineData("startDate", "not-a-date")]
    [InlineData("endDate", "2025-12-31T00:00:00Z")]
    [InlineData("startDate", null)]
    [InlineData("name", " ")]
    public async Task InvalidProjectInputCannotChangeExistingProject(string field, object? value)
    {
        var employee = await Employee();
        var project = await Project(Id(employee));
        var body = ProjectBody(Id(employee));
        body[field] = value;
        await AssertProblem(await _client.PostAsJsonAsync("/api/v1/projects", body), HttpStatusCode.BadRequest);
        await AssertProblem(await _client.PutAsJsonAsync($"/api/v1/projects/{Id(project)}", body), HttpStatusCode.BadRequest);
        await using var db = Database();
        Assert.Equal(1, await db.Projects.CountAsync());
        Assert.Equal("Test Project", (await db.Projects.SingleAsync()).Name);
    }

    [Theory]
    [InlineData("projectId", 999)]
    [InlineData("assignedEmployeeId", 999)]
    [InlineData("priority", 99)]
    [InlineData("status", 99)]
    [InlineData("dueDate", "2026-06-01T00:00:00")]
    [InlineData("dueDate", null)]
    [InlineData("title", " ")]
    public async Task InvalidTaskInputCannotChangeExistingTask(string field, object? value)
    {
        var employee = await Employee();
        var project = await Project(Id(employee));
        var task = await TaskItem(Id(project), Id(employee));
        var body = TaskBody(Id(project), Id(employee));
        body[field] = value;
        await AssertProblem(await _client.PostAsJsonAsync("/api/v1/tasks", body), HttpStatusCode.BadRequest);
        await AssertProblem(await _client.PutAsJsonAsync($"/api/v1/tasks/{Id(task)}", body), HttpStatusCode.BadRequest);
        await using var db = Database();
        Assert.Equal(1, await db.ProjectTasks.CountAsync());
        Assert.Equal("Test Task", (await db.ProjectTasks.SingleAsync()).Title);
    }

    [Fact]
    public async Task RequiredDatesAndEnumsCannotBeOmitted()
    {
        var employee = await Employee();
        var projectBody = ProjectBody(Id(employee));
        projectBody.Remove("endDate");
        projectBody.Remove("status");
        await AssertProblem(await _client.PostAsJsonAsync("/api/v1/projects", projectBody), HttpStatusCode.BadRequest);
        var project = await Project(Id(employee));
        var taskBody = TaskBody(Id(project), Id(employee));
        taskBody.Remove("priority");
        taskBody.Remove("dueDate");
        await AssertProblem(await _client.PostAsJsonAsync("/api/v1/tasks", taskBody), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OverlengthInputAndMalformedJsonReturnSafeProblems()
    {
        await AssertProblem(await _client.PostAsJsonAsync("/api/v1/employees", new { fullName = new string('x', 201), email = "long@example.test" }), HttpStatusCode.BadRequest);
        await AssertProblem(await _client.PostAsync("/api/v1/employees", new StringContent("{broken", Encoding.UTF8, "application/json")), HttpStatusCode.BadRequest);
        await AssertProblem(await _client.PostAsync("/api/v1/tasks", new StringContent("null", Encoding.UTF8, "application/json")), HttpStatusCode.BadRequest);
        await using var db = Database();
        Assert.Empty(await db.Employees.ToListAsync());
    }

    [Fact]
    public async Task ValidNumericEnumsAndDueDatesOutsideProjectWindowAreSupported()
    {
        var employee = await Employee();
        var body = ProjectBody(Id(employee));
        body["status"] = 1;
        body["endDate"] = body["startDate"]; // Same-day projects are allowed.
        var response = await _client.PostAsJsonAsync("/api/v1/projects", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var project = await Json(response);
        Assert.Equal("Active", project.GetProperty("status").GetString());
        var taskBody = TaskBody(Id(project), Id(employee));
        taskBody["priority"] = 3;
        taskBody["status"] = 1;
        response = await _client.PostAsJsonAsync("/api/v1/tasks", taskBody);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Critical", (await Json(response)).GetProperty("priority").GetString());
    }

    [Fact]
    public async Task ListUsesTwoSqlQueriesWithServerPagingAndJoinedRelatedDtos()
    {
        var employee = await Employee();
        await Project(Id(employee), "One", "Active");
        await Project(Id(employee), "Two", "Active");
        _queries.Commands.Clear();
        var response = await _client.GetAsync($"/api/v1/projects?managerId={Id(employee)}&status=Active&page=2&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single((await Json(response)).GetProperty("items").EnumerateArray());
        Assert.Equal(2, _queries.Commands.Count);
        var sql = string.Join("\n", _queries.Commands);
        Assert.Contains("COUNT(*)", sql);
        Assert.Contains("WHERE", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.Contains("OFFSET", sql);
        Assert.Contains("FETCH NEXT", sql);
        Assert.Contains("JOIN", sql);
    }

    [Fact]
    public async Task UpdatesCanChangeManagerProjectAndAssigneeToValidActiveReferences()
    {
        var oldEmployee = await Employee();
        var newEmployee = await Employee();
        var oldProject = await Project(Id(oldEmployee));
        var newProject = await Project(Id(newEmployee));
        var task = await TaskItem(Id(oldProject), Id(oldEmployee));
        var projectResponse = await _client.PutAsJsonAsync($"/api/v1/projects/{Id(oldProject)}", ProjectBody(Id(newEmployee)));
        Assert.Equal(HttpStatusCode.OK, projectResponse.StatusCode);
        Assert.Equal(Id(newEmployee), Id((await Json(projectResponse)).GetProperty("manager")));
        var taskResponse = await _client.PutAsJsonAsync($"/api/v1/tasks/{Id(task)}", TaskBody(Id(newProject), Id(newEmployee)));
        Assert.Equal(HttpStatusCode.OK, taskResponse.StatusCode);
        var changed = await Json(taskResponse);
        Assert.Equal(Id(newProject), Id(changed.GetProperty("project")));
        Assert.Equal(Id(newEmployee), Id(changed.GetProperty("assignedEmployee")));
        await using var db = Database();
        Assert.Equal(Id(newProject), (await db.ProjectTasks.SingleAsync()).ProjectId);
        Assert.Equal(Id(newEmployee), (await db.Projects.SingleAsync(p => p.Id == Id(oldProject))).ManagerId);
    }

    private sealed class QueryRecorder : DbCommandInterceptor
    {
        public ConcurrentQueue<string> Commands { get; } = new();
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Commands.Enqueue(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }

    [Fact]
    public async Task UnhandledDatabaseErrorsDoNotLeakDetailsEvenInDevelopment()
    {
        await using (var db = Database())
            await db.Database.ExecuteSqlRawAsync("DROP INDEX IX_Employees_Email ON Employees; ALTER TABLE Employees DROP CONSTRAINT CK_Employees_Email;");
        // Force an actual database failure in this disposable fixture by renaming a column.
        await using (var db = Database())
            await db.Database.ExecuteSqlRawAsync("EXEC sp_rename 'Employees.Email', 'UnavailableEmail', 'COLUMN'");
        using var development = _factory!.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = development.CreateClient();
        await AssertProblem(await client.GetAsync("/api/v1/employees"), HttpStatusCode.InternalServerError);
    }
}
