using System;
using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models
{
    public class WorkClaim
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Lecturer User ID")]
        public string WorkerUserId { get; set; } // Keep property name for DB compatibility

        [Required]
        [Display(Name = "First Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string Surname { get; set; }

        [Display(Name = "Department/Faculty")]
        public string? Department { get; set; }

        [Required]
        [Range(50, 2000, ErrorMessage = "Hourly rate must be between R50 and R2000")]
        [Display(Name = "Hourly Rate (R)")]
        public decimal RatePerJob { get; set; } // Keep property name for DB compatibility

        [Required]
        [Range(0.5, 500, ErrorMessage = "Hours must be between 0.5 and 500")]
        [Display(Name = "Total Hours Worked")]
        public int NumberOfJobs { get; set; } // Keep property name for DB compatibility, but represents hours

        [Display(Name = "Total Claim Amount")]
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Submitted";

        [Display(Name = "Rejection Reason")]
        public string? RejectReason { get; set; }

        public bool ReasonRequired { get; set; } = false;

        [Display(Name = "Submission Date")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}