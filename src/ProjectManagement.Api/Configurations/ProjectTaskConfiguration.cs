using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Api.Models;

namespace ProjectManagement.Api.Configurations;

public class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        builder.ToTable("ProjectTasks", table =>
        {
            table.HasCheckConstraint("CK_ProjectTasks_Title", "LEN(LTRIM(RTRIM([Title]))) > 0");
            table.HasCheckConstraint("CK_ProjectTasks_Priority", "[Priority] IN (0, 1, 2, 3)");
            table.HasCheckConstraint("CK_ProjectTasks_Status", "[Status] IN (0, 1, 2, 3)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.DueDate).IsRequired().HasColumnType("datetime2(7)");
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.Priority).IsRequired().HasConversion<int>();
        builder.HasOne(x => x.Project).WithMany(x => x.Tasks)
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.AssignedEmployee).WithMany(x => x.AssignedTasks)
            .HasForeignKey(x => x.AssignedEmployeeId).OnDelete(DeleteBehavior.NoAction);
    }
}
