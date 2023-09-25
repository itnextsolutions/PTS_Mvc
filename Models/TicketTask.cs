using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MVC_BugTracker.Models
{
    public class TicketTask
    {
        //[Key]
        //public int? TicketTaskId { get; set; }


        [Key]
        public int? TicketTaskId { get; set; }

        [Display(Name = "Task Title")]
        [Required(ErrorMessage = "Task Title is required")]
        public string TaskTitle { get; set; }

        [Display(Name = "Task Description")]
        [Required(ErrorMessage = "Task Description is required")]
        public string TaskDescription { get; set; }

        //public bool IsDeleted { get; set; }
        public int TicketId { get; set; }
        public virtual Ticket Ticket { get; set; }
    }

    //public class TicketViewModel
    //{
    //    //public List<string> TaskTitle { get; set; }

    //    //public List<string> TaskDescription { get; set; }

    //    public List<TicketTask> TicketTask { get; set; }
    //}
}
