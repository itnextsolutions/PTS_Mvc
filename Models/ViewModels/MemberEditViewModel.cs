using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace MVC_BugTracker.Models.ViewModels
{
    public class MemberEditViewModel
    {

       public string Id { get; set; }

        public string UserName { get; set; }

        public string PhoneNumber { get; set; }

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

           
         

            [Display(Name = "Shift")]
            public int ShiftId { get; set; }

            public SelectList ShiftList { get; set; }


            [Required]
            [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[$@$!%*?&])[A-Za-z\\d$@$!%*?&]{6,}$",
        ErrorMessage = "Minimum 6 characters atleast 1 lowercase, 1 Uppercase, 1 Number and 1 Special Character")]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            
        }
    }




