using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SchoolProject.Models;

namespace SchoolProject.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<LecturerModule> LecturerModules { get; set; }
        public DbSet<StudentModule> StudentModules { get; set; }
        public DbSet<RememberedDevice> RememberedDevices { get; set; }
        public DbSet<AssessmentType> AssessmentTypes { get; set; }
        public DbSet<Assessment> Assessments { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // === Conversions ===
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    var clrType = property.ClrType;

                    // Convert enums to strings
                    if (clrType.IsEnum)
                    {
                        var converterType = typeof(EnumToStringConverter<>).MakeGenericType(clrType);
                        var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                        property.SetValueConverter(converter);
                    }

                    // Convert bools to strings
                    if (clrType == typeof(bool))
                    {
                        var boolConverter = new ValueConverter<bool, string>(
                            v => v.ToString(),
                            v => bool.Parse(v)
                        );
                        property.SetValueConverter(boolConverter);
                        property.SetMaxLength(5);
                    }
                }
            }

            // === Relationships ===
            modelBuilder.Entity<StudentModule>()
                .HasOne(sm => sm.LecturerModule)
                .WithMany()
                .HasForeignKey(sm => sm.LecturerModuleID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StudentModule>()
                .HasOne(sm => sm.Student)
                .WithMany()
                .HasForeignKey(sm => sm.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // === Seeding Accounts ===
            modelBuilder.Entity<Account>().HasData(
                new Account
                {
                    UserID = 1,
                    Name = "Admin",
                    Surname = "User",
                    Title = "System Admin",
                    Role = UserRole.Administrator,
                    Email = "admin@school.com",
                    Password = "admin123",
                    UserStatus = UserStatus.Active
                },
                new Account
                {
                    UserID = 2,
                    Name = "Lecturer",
                    Surname = "User",
                    Title = "System Lecturer",
                    Role = UserRole.Lecturer,
                    Email = "lecturer@school.com",
                    Password = "admin123",
                    UserStatus = UserStatus.Active
                },
                new Account
                {
                    UserID = 3,
                    Name = "Student",
                    Surname = "User",
                    Title = "System Student",
                    Role = UserRole.Student,
                    Email = "student@school.com",
                    Password = "admin123",
                    UserStatus = UserStatus.Active
                }
            );

            // === Seeding Modules ===
            modelBuilder.Entity<Module>().HasData(
                new Module { ModuleID = 1, ModuleName = "Mathematics 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 12 },
                new Module { ModuleID = 2, ModuleName = "Physics 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 14 },
                new Module { ModuleID = 3, ModuleName = "Chemistry 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 15 },
                new Module { ModuleID = 4, ModuleName = "Biology 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 10 },
                new Module { ModuleID = 5, ModuleName = "Computer Science 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 16 }
                
                
            );

            // === Seeding AssessmentTypes ===
            modelBuilder.Entity<AssessmentType>().HasData(
                new AssessmentType { AssessmentTypeID = 1, AssessmentTypeDescription = "Formal written examination", AssessmentTypeStatus = AssessmentTypeStatus.Active },
                new AssessmentType { AssessmentTypeID = 2, AssessmentTypeDescription = "Short knowledge test", AssessmentTypeStatus = AssessmentTypeStatus.Active },
                new AssessmentType { AssessmentTypeID = 3, AssessmentTypeDescription = "Practical or theoretical work assignment", AssessmentTypeStatus = AssessmentTypeStatus.Active },
                new AssessmentType { AssessmentTypeID = 4, AssessmentTypeDescription = "Extended practical project work", AssessmentTypeStatus = AssessmentTypeStatus.Active },
                new AssessmentType { AssessmentTypeID = 5, AssessmentTypeDescription = "Oral presentation of work", AssessmentTypeStatus = AssessmentTypeStatus.Active },
                new AssessmentType { AssessmentTypeID = 6, AssessmentTypeDescription = "Hands-on practical assessment", AssessmentTypeStatus = AssessmentTypeStatus.Active },
                new AssessmentType { AssessmentTypeID = 7, AssessmentTypeDescription = "Analysis of real-world scenarios", AssessmentTypeStatus = AssessmentTypeStatus.Active },
                new AssessmentType { AssessmentTypeID = 8, AssessmentTypeDescription = "Collection of work samples", AssessmentTypeStatus = AssessmentTypeStatus.Active },
                new AssessmentType { AssessmentTypeID = 9, AssessmentTypeDescription = "Scientific laboratory report", AssessmentTypeStatus = AssessmentTypeStatus.Active },
                new AssessmentType { AssessmentTypeID = 10, AssessmentTypeDescription = "Academic research paper", AssessmentTypeStatus = AssessmentTypeStatus.Active }
            );
        }
    }
}
