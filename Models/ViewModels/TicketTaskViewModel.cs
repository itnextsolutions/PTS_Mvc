using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MVC_BugTracker.Models.ViewModels
{
    //public class TicketTaskViewModel
    //{

    //}

    // TicketViewModel.cs
    public class TicketTaskViewModel
    {
        public Ticket tic { get; set; } = new ();
        //public string Title { get; set; }
        //public string Description { get; set; }
        //public List<TaskViewModel> TaskViewModels { get; set; }

        //[Required(ErrorMessage = "Taskview Title is required.")]
        public List<TicketTask> TaskViewModels { get; set; }
    }

    //public class TaskViewModel
    //{
    //    public string TaskTitle { get; set; }
    //    public string TaskDescription { get; set; }

   
    //}

}
