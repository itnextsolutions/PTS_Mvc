using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVC_BugTracker.Models
{
    public class RotationShift
    {
        public int Id { get; set; }

        [Display(Name = "Type of Shift")]
        public string Shift_type { get; set; }

        [Display(Name = "Shift Start")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Display(Name = "Shift End")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        public int? CompanyId { get; set; }

        [NotMapped]
        [Display(Name = "Shift Duration")]
        public string ShiftInterval
        {
            get
            {
                return $"{Shift_type} ({StartTime}-{EndTime})";
            }
        }
    }
}
