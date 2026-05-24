namespace SchoolProject.Status
{
    public enum AssessmentStatus
    {
        NotStarted,
        Completed,
        Missed,
        Rescheduled
    }


    public enum ModLecturerStatus
    {
        Active,
        Inactive
    }

    public enum ModuleType
    {
        Core,
        Elective
    }

    public enum ModuleStatus
    {
        Active,
        Inactive
    }


    public enum StudModStatus
    {
        Active,
        Inactive,
        Completed
    }

    public enum UserRole
    {
        Administrator,
        Lecturer,
        Student,
        Parent
    }

    public enum UserStatus
    {
        Active,
        Inactive,
        Suspended
    }

    public enum NotificationType
    {
        General,
        AssessmentCreated,
        DeadlineReminder,
        AssessmentMissed,
        AssessmentCompleted,
        AssessmentRescheduled
    }

}
