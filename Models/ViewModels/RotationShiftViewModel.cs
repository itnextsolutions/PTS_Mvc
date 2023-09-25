using System.ComponentModel.DataAnnotations;

namespace MVC_BugTracker.Models.ViewModels
{
    public class RotationShiftViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Type of Shift")]
        public string Shift_type { get; set; }

        [Display(Name = "Shift Start")]
        public string StartTimeFormatted { get; set; }

        [Display(Name = "Shift End")]
        public string EndTimeFormatted { get; set; }
    }
}
