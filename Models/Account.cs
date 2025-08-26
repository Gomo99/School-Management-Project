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



        public DateTime? ResetPinExpiration { get; set; }
        public ICollection<LecturerModule> LecturerModules { get; set; }
        public ICollection<StudentModule> StudentModules { get; set; }
    }

    public enum UserRole
    {
        Administrator,
        Lecturer,
        Student
    }

    public enum UserStatus
    {
        Active,
        Inactive,
        Suspended
    }
}
