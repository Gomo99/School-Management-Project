using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SchoolProject.Data;
using SchoolProject.Models;
using Microsoft.EntityFrameworkCore;
using SchoolProject.Status;

namespace SchoolProject.Service
{
    public class AssessmentDeadlineNotificationService : BackgroundService
    {
        private readonly IServiceProvider _services;

        public AssessmentDeadlineNotificationService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckAndNotify();
                // Run every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task CheckAndNotify()
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notifService = scope.ServiceProvider.GetRequiredService<NotificationService>();

            // Find assessments due within 24 hours that are not yet completed
            var upcomingAssessments = await context.Assessments
                .Include(a => a.StudentModule)
                    .ThenInclude(sm => sm.Student)
                .Include(a => a.StudentModule)
                    .ThenInclude(sm => sm.LecturerModule)
                        .ThenInclude(lm => lm.Module)
                .Where(a => !a.IsDeleted
                    && a.DueDate >= DateTime.Now
                    && a.DueDate <= DateTime.Now.AddHours(24)
                    && a.AssessmentStatus != AssessmentStatus.Completed)
                .ToListAsync();

            foreach (var assessment in upcomingAssessments)
            {
                var student = assessment.StudentModule?.Student;
                if (student == null) continue;

                string message = $"Reminder: \"{assessment.StudentModule?.LecturerModule?.Module?.ModuleName}\" " +
                                 $"– {assessment.AssessmentType?.AssessmentTypeDescription} " +
                                 $"is due on {assessment.DueDate:dd MMM yyyy at HH:mm}.";

                await notifService.CreateAsync(student.UserID, message, "deadline_reminder");
            }
        }
    }
}