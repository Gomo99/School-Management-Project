using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.EntityFrameworkCore;
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
        public DbSet<SupportMessage> SupportMessages { get; set; }

        public DbSet<AssessmentType> assessmentTypes { get; set; }
        public DbSet<Assessment> Assessments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>()
                .Property(a => a.Role)
                .HasConversion<string>();

            modelBuilder.Entity<Account>()
                .Property(a => a.UserStatus)
                .HasConversion<string>();

            modelBuilder.Entity<Module>()
                .Property(a => a.ModuleType)
                .HasConversion<string>(); // Convert ModuleType enum to string

            modelBuilder.Entity<Module>()
                .Property(a => a.ModuleStatus)
                .HasConversion<string>(); // Convert ModuleStatus enum to string

            modelBuilder.Entity<LecturerModule>()
                .Property(lm => lm.ModLecturerStatus)
                .HasConversion<string>();


            modelBuilder.Entity<AssessmentType>()
                            .Property(a => a.AssessmentTypeStatus)
                            .HasConversion<string>();


            modelBuilder.Entity<Assessment>()
                .Property(a => a.AssessmentStatus)
                .HasConversion<string>();







            modelBuilder.Entity<StudentModule>()
                           .Property(lm => lm.StudModStatus)
                           .HasConversion<string>();


            modelBuilder.Entity<StudentModule>()
        .HasOne(sm => sm.LecturerModule)
        .WithMany()
        .HasForeignKey(sm => sm.LecturerModuleID)
        .OnDelete(DeleteBehavior.NoAction);  // Prevent cascading delete on LecturerModule

            modelBuilder.Entity<StudentModule>()
                .HasOne(sm => sm.Student)
                .WithMany()
                .HasForeignKey(sm => sm.UserID)
                .OnDelete(DeleteBehavior.NoAction);










            // Seeding the Modules table with 50 modules
            modelBuilder.Entity<Module>().HasData(
     new Module { ModuleID = 1, ModuleName = "Mathematics 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 12 },
     new Module { ModuleID = 2, ModuleName = "Physics 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 14 },
     new Module { ModuleID = 3, ModuleName = "Chemistry 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 15 },
     new Module { ModuleID = 4, ModuleName = "Biology 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 10 },
     new Module { ModuleID = 5, ModuleName = "Computer Science 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 16 },
     new Module { ModuleID = 6, ModuleName = "English Literature", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 12 },
     new Module { ModuleID = 7, ModuleName = "History 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 18 },
     new Module { ModuleID = 8, ModuleName = "Philosophy 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 12 },
     new Module { ModuleID = 9, ModuleName = "Psychology 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 12 },
     new Module { ModuleID = 10, ModuleName = "Sociology 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 14 },
     new Module { ModuleID = 11, ModuleName = "Political Science 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 12 },
     new Module { ModuleID = 12, ModuleName = "Economics 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 16 },
     new Module { ModuleID = 13, ModuleName = "Geography 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 13 },
     new Module { ModuleID = 14, ModuleName = "Engineering 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 20 },
     new Module { ModuleID = 15, ModuleName = "Law 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 12 },
     new Module { ModuleID = 16, ModuleName = "Medicine 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 18 },
     new Module { ModuleID = 17, ModuleName = "Nursing 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 14 },
     new Module { ModuleID = 18, ModuleName = "Business Management 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 14 },
     new Module { ModuleID = 19, ModuleName = "Accounting 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 12 },
     new Module { ModuleID = 20, ModuleName = "Marketing 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 13 },
     new Module { ModuleID = 21, ModuleName = "Design 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 12 },
     new Module { ModuleID = 22, ModuleName = "Art 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 12 },
     new Module { ModuleID = 23, ModuleName = "Music 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 14 },
     new Module { ModuleID = 24, ModuleName = "Theater 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 13 },
     new Module { ModuleID = 25, ModuleName = "Dance 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 12 },
     new Module { ModuleID = 26, ModuleName = "Architecture 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 24 },
     new Module { ModuleID = 27, ModuleName = "Physics 201", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 15 },
     new Module { ModuleID = 28, ModuleName = "Statistics 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 12 },
     new Module { ModuleID = 29, ModuleName = "Data Science 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 14 },
     new Module { ModuleID = 30, ModuleName = "Artificial Intelligence 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 16 },
     new Module { ModuleID = 31, ModuleName = "Machine Learning 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 14 },
     new Module { ModuleID = 32, ModuleName = "Cloud Computing 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 16 },
     new Module { ModuleID = 33, ModuleName = "Cyber Security 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 16 },
     new Module { ModuleID = 34, ModuleName = "Networking 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 12 },
     new Module { ModuleID = 35, ModuleName = "Database Management 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 15 },
     new Module { ModuleID = 36, ModuleName = "Web Development 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 14 },
     new Module { ModuleID = 37, ModuleName = "Game Development 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 14 },
     new Module { ModuleID = 38, ModuleName = "Cloud Architecture 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 15 },
     new Module { ModuleID = 39, ModuleName = "Blockchain 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 18 },
     new Module { ModuleID = 40, ModuleName = "Digital Marketing 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 12 },
     new Module { ModuleID = 41, ModuleName = "Human Resources 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 13 },
     new Module { ModuleID = 42, ModuleName = "Project Management 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 14 },
     new Module { ModuleID = 43, ModuleName = "Operations Management 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 15 },
     new Module { ModuleID = 44, ModuleName = "Entrepreneurship 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 12 },
     new Module { ModuleID = 45, ModuleName = "Public Relations 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 12 },
     new Module { ModuleID = 46, ModuleName = "Leadership 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 13 },
     new Module { ModuleID = 47, ModuleName = "Sustainability 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 14 },
     new Module { ModuleID = 48, ModuleName = "Logistics 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 12 },
     new Module { ModuleID = 49, ModuleName = "Supply Chain Management 101", ModuleType = ModuleType.Core, ModuleStatus = ModuleStatus.Active, Duration = 16 },
     new Module { ModuleID = 50, ModuleName = "Business Analytics 101", ModuleType = ModuleType.Elective, ModuleStatus = ModuleStatus.Active, Duration = 15 }
 );

        }

    }
}
