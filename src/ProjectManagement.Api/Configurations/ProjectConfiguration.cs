using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Api.Models;

namespace ProjectManagement.Api.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects", table =>
        {
            table.HasCheckConstraint("CK_Projects_Name", "LEN(LTRIM(RTRIM([Name]))) > 0");
            table.HasCheckConstraint("CK_Projects_Dates", "[EndDate] >= [StartDate]");
            table.HasCheckConstraint("CK_Projects_Status", "[Status] IN (0, 1, 2, 3)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.StartDate).IsRequired().HasColumnType("datetime2(7)");
        builder.Property(x => x.EndDate).IsRequired().HasColumnType("datetime2(7)");
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.HasOne(x => x.Manager).WithMany(x => x.ManagedProjects)
            .HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.NoAction);
    }
}
