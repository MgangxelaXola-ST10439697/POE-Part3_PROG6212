using System;
using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        [Display(Name = "Target Role")]
        public string TargetRole { get; set; }

        [Display(Name = "Related Claim")]
        public int? WorkClaimId { get; set; }

        [Display(Name = "Read Status")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
