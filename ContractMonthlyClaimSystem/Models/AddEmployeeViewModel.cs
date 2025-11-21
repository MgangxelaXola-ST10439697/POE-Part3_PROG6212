using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models
{
    public class AddEmployeeViewModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Surname { get; set; }

        public string? Department { get; set; }

        [Required]
        [Range(50, 2000, ErrorMessage = "Hourly rate must be between R50 and R2000")]
        [Display(Name = "Default Hourly Rate (R)")]
        public decimal DefaultRatePerJob { get; set; }

        [Required]
        [Display(Name = "Role")]
        public string RoleName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Temporary Password")]
        public string TempPassword { get; set; }
    }
}