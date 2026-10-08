using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Api.Models;

namespace ProjectManagement.Api.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees", table =>
        {
            table.HasCheckConstraint("CK_Employees_FullName", "LEN(LTRIM(RTRIM([FullName]))) > 0");
            table.HasCheckConstraint("CK_Employees_Email", "LEN(LTRIM(RTRIM([Email]))) > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(320).UseCollation("Latin1_General_100_CI_AS_SC");
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true).HasSentinel(true);
    }
}
