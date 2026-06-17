using SchoolProject.Status;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProject.Models
{
    public class Account
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Surname { get; set; }

        [StringLength(20)]
        public string Title { get; set; }

        [Required]
        [EnumDataType(typeof(UserRole))]
        public UserRole Role { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [NotMapped]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [EnumDataType(typeof(UserStatus))]
        public UserStatus UserStatus { get; set; } = UserStatus.Inactive;

        public string? ResetPin { get; set; }


        public bool IsTwoFactorEnabled { get; set; } = false;
        public string? TwoFactorSecretKey { get; set; }
        public string? TwoFactorRecoveryCodes { get; set; }


        public string? EmailVerificationTokenHash { get; set; }
        public DateTime? EmailVerificationTokenExpires { get; set; }


        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }


        public string? ExternalProvider { get; set; }
        public string? ExternalProviderId { get; set; }
        public DateTime? LastExternalLogin { get; set; }


        public DateTime? ResetPinExpiration { get; set; }
        public ICollection<LecturerModule> LecturerModules { get; set; }
        public ICollection<StudentModule> StudentModules { get; set; }
    }



    public class Assessment
    {
        [Key]
        public int AssessmentID { get; set; }

        [Required]
        public int StudentModuleID { get; set; }

        [Required]
        public int AssessmentTypeID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Required]
        public AssessmentStatus AssessmentStatus { get; set; } = AssessmentStatus.NotStarted;

        // Soft delete flag
        public bool IsDeleted { get; set; } = false;

        [ForeignKey("StudentModuleID")]
        public virtual StudentModule StudentModule { get; set; }

        [ForeignKey("AssessmentTypeID")]
        public virtual AssessmentType AssessmentType { get; set; }
    }


    public class AssessmentType
    {
        [Key]
        public int AssessmentTypeID { get; set; }

        [Required]
        [StringLength(100)]
        public string AssessmentTypeDescription { get; set; }

        public bool IsDeleted { get; set; } = false;   // soft delete
    }



    public class LecturerModule
    {
        [Key]
        public int LecturerModuleID { get; set; }

        [Required]
        public int ModuleID { get; set; }

        [ForeignKey("ModuleID")]
        public Module Module { get; set; }

        [Required]
        public int UserID { get; set; }

        [ForeignKey("UserID")]
        public Account Lecturer { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [Required]
        public ModLecturerStatus ModLecturerStatus { get; set; }
    }


    public class Message
    {
        [Key]
        public int MessageId { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Content { get; set; }

        [Required]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; }

        [Required]
        public bool IsDeletedBySender { get; set; } = false;

        [Required]
        public bool IsDeletedByReceiver { get; set; } = false;

        // Navigation properties
        [ForeignKey("SenderId")]
        public virtual Account Sender { get; set; }

        [ForeignKey("ReceiverId")]
        public virtual Account Receiver { get; set; }


        public bool IsRead { get; set; } = false;



    }



    public class Module
    {
        [Key]
        public int ModuleID { get; set; }

        [Required]
        [StringLength(100)]
        public string ModuleName { get; set; }

        [Required]
        [Range(1, 52, ErrorMessage = "Duration must be between 1 and 52 weeks.")]
        public int Duration { get; set; }

        [Required]
        public ModuleType ModuleType { get; set; }

        [Required]
        public ModuleStatus ModuleStatus { get; set; }

        public ICollection<LecturerModule>? LecturerModules { get; set; }
        public ICollection<StudentModule>? StudentModules { get; set; }
    }



    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        [Required]
        public int UserID { get; set; }           // Recipient

        [Required]
        [MaxLength(500)]
        public string Message { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public NotificationType Type { get; set; } = NotificationType.General;

        public int? ReferenceID { get; set; }      // e.g., AssessmentID for deep‑linking

        [ForeignKey("UserID")]
        public Account User { get; set; }
    }



    public class RememberedDevice
    {
        public int Id { get; set; }

        public int UserId { get; set; }          // FK to Account.UserID
        public string TokenHash { get; set; } = null!; // SHA256(Base64) of the raw token
        public DateTime ExpiresAt { get; set; }

        public string? DeviceName { get; set; }   // optional: "Chrome on Windows", etc.
        public string? UserAgent { get; set; }    // optional: Request UA for admin/user display
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Revoked { get; set; } = false;

        public Account? User { get; set; }
    }



    public class StudentModule
    {
        // Surrogate Key
        [Key]
        public int StudentModuleID { get; set; }

        // Foreign Key: Reference to LecturerModule
        [Required]
        public int LecturerModuleID { get; set; }

        // Navigation Property for LecturerModule
        [ForeignKey("LecturerModuleID")]
        public LecturerModule LecturerModule { get; set; }

        // Foreign Key: Reference to User (Student)
        [Required]
        public int UserID { get; set; }

        // Navigation Property for Account (Student)
        [ForeignKey("UserID")]
        public Account Student { get; set; }

        // Date of enrollment
        public DateTime Date { get; set; } = DateTime.Now;

        // Status of enrollment (e.g., Active, Completed, Inactive)
        [Required]
        public StudModStatus StudModStatus { get; set; }



    }


    public class StudentParentLink
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ParentUserId { get; set; }   // FK to Account (Parent)

        [Required]
        public int StudentUserId { get; set; }  // FK to Account (Student)

        [ForeignKey("ParentUserId")]
        public virtual Account Parent { get; set; }

        [ForeignKey("StudentUserId")]
        public virtual Account Student { get; set; }
    }


}
