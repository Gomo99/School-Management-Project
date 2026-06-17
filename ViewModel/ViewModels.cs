using Microsoft.AspNetCore.Mvc.Rendering;
using SchoolProject.Models;
using SchoolProject.Status;
using System.ComponentModel.DataAnnotations;

namespace SchoolProject.ViewModel
{
    public class AssignLecturerModulesViewModel
    {
        public int UserID { get; set; }
        public List<int> SelectedModuleIDs { get; set; } = new List<int>();
        public DateTime AssignedDate { get; set; } = DateTime.Now;
    }


    public class AttachmentViewModel
    {
        public int AttachmentId { get; set; }
        public string FileName { get; set; }
        public string Url { get; set; }
        public long Size { get; set; }
    }


    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }


    public class DashboardViewModel
    {
        public int ActiveModulesCount { get; set; }
        public int InactiveModulesCount { get; set; }
        public int TotalModules => ActiveModulesCount + InactiveModulesCount;
        public double ActivePercentage =>
        TotalModules == 0 ? 0 : (double)ActiveModulesCount / TotalModules * 100;

        public double InactivePercentage =>
            TotalModules == 0 ? 0 : (double)InactiveModulesCount / TotalModules * 100;

        public List<Account> RecentAccounts { get; set; } = new();
    }


    public class DisableTwoFactorViewModel
    {
        [Required]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [Required]
        [Display(Name = "Verification Code")]
        public string VerificationCode { get; set; }
    }


    public class EditProfileViewModel
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Surname { get; set; }

        [StringLength(20)]
        public string Title { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }


    }


    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }



    public class MessageViewModel
    {
        public int MessageId { get; set; }
        public string SenderName { get; set; }
        public string ReceiverName { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsRead { get; set; }

        public int ReceiverId { get; set; }
        public int SenderId { get; set; }


    }


    public class RecoveryLoginViewModel
    {
        [Required]
        [Display(Name = "Recovery Code")]
        public string RecoveryCode { get; set; }
    }


    public class SendMessageViewModel
    {
        [Required]
        [Display(Name = "Recipient")]
        public int ReceiverId { get; set; }

        [Required]
        [MaxLength(500)]
        [Display(Name = "Message")]
        public string Content { get; set; }

    }

    public class TwoFactorLoginViewModel
    {
        [Required]
        [Display(Name = "Verification Code")]
        public string VerificationCode { get; set; }

        public bool UseRecoveryCode { get; set; } = false;

        [Display(Name = "Recovery Code")]
        public string RecoveryCode { get; set; }
    }

    public class TwoFactorSetupViewModel
    {
        public string QrCodeImageUrl { get; set; }
        public string ManualEntryKey { get; set; }
        public string VerificationCode { get; set; }
        public List<string> RecoveryCodes { get; set; } = new List<string>();

    }


    public class UserManagementViewModel
    {
        public List<Account> Users { get; set; }
        public string SearchTerm { get; set; }
        public UserRole? RoleFilter { get; set; }
        public UserStatus? StatusFilter { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<SelectListItem> Roles { get; set; }
        public List<SelectListItem> Statuses { get; set; }
    }



    public class VerificationCodeLoginViewModel
    {
        [Required]
        [Display(Name = "Verification Code")]
        public string VerificationCode { get; set; }

        public bool RememberThisDevice { get; set; }

    }



    public class ViewProfileViewModel
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string iTitle { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string UserStatus { get; set; }
        public string Title { get; set; }
        public bool IsTwoFactorEnabled { get; set; }
    }
}
