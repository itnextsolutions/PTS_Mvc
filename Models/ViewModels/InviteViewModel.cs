using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MVC_BugTracker.Models.ViewModels
{
    public class InviteViewModel
    {
        //public BTUser user { get; set; } = new();

        [Display(Name = "Company")]
        public Company Company { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "The  ClientId is required.")]
        [Display(Name = "Client*")]
        public int ProjectId { get; set; }

        public SelectList ProjectsList { get; set; }
        [Required]

        [Display(Name = "Message*")]
        public string Message { get; set; }

        [Display(Name = "Shift*")]
        public int ShiftId { get; set; }

        public SelectList ShiftList { get; set; }



        //[Display(Name = "Shift Start Date")]
        //[DataType(DataType.DateTime)]
        //[Required]
        //public DateTimeOffset? StartDate { get; set; }

        //[Display(Name = "Shift End Date")]
        //[DataType(DataType.DateTime)]
        //[Required]
        //public DateTimeOffset? DueDate { get; set; }


        [Required]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[$@$!%*?&.])[A-Za-z\\d$@$!%*?&.]{6,}$",
    ErrorMessage="Minimum 6 characters atleast 1 lowercase, 1 Uppercase, 1 Number and 1 Special Character")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password*")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password*")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
