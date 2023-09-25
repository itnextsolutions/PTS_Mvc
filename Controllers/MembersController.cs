using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC_BugTracker.Data;
using MVC_BugTracker.Models.Enums;
using MVC_BugTracker.Models;
using MVC_BugTracker.Services.Interfaces;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using MVC_BugTracker.Extensions;
using System.Linq;
using System.Xml.Linq;
using MVC_BugTracker.Models.ViewModels;
using System.ComponentModel.Design;

namespace MVC_BugTracker.Controllers
{
    [Authorize]
    [Authorize(Roles = "Admin")]
    public class MembersController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly IBTCompanyInfoService _infoService;
        private readonly IBTProjectService _projectService;
        private readonly IBTTicketService _ticketService;
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTHistoryService _historyService;
        private readonly IBTCompanyInfoService _companyService;
        private readonly IBTNotificationService _notificationService;
        private readonly IBTRolesService _roleService;
        private readonly SignInManager<BTUser> _signInManager;

        public MembersController(ApplicationDbContext context,
                                 IBTCompanyInfoService infoService,
                                 IBTTicketService ticketService,
                                 UserManager<BTUser> userManager,
                                 IBTProjectService projectService,
                                 IBTHistoryService historyService,
                                 IBTCompanyInfoService companyService,
                                 IBTNotificationService notificationService,
                                 IBTRolesService roleService)
        {
            _context = context;
            _infoService = infoService;
            _ticketService = ticketService;
            _userManager = userManager;
            _projectService = projectService;
            _historyService = historyService;
            _companyService = companyService;
            _notificationService = notificationService;
            _roleService = roleService;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> AllMembers()
        {
            // GET company id
            int companyId = User.Identity.GetCompanyId().Value;

            List<BTUser> users = new();

            try
            {
                users = await _infoService.GetAllMembersAsync(companyId);
                var shiftList=await _context.RotationShift.Where(x=>x.CompanyId==companyId).ToListAsync();
               
                foreach (var user in users)
                    {
                    var company = await _context.RotationShift.FindAsync(Convert.ToInt32(user.shift));
                    user.shift = company.Shift_type;
                }
               
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"*** ERROR *** - Error getting all tickets - {ex.Message}");
                throw;
            }

            return View(users);
        }




        // GET: Tickets/Details/5
        //[Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Details(string id)
        {
            var user = await _context.Users
            .Where(u => u.Id == id)
            .FirstOrDefaultAsync();

     
            var userShift =  _context.RotationShift.Where(x => x.Id == Convert.ToInt32(user.shift)).FirstOrDefault();

            user.shift=userShift.ShiftInterval;
            //if (id == null)
            //{
            //    return NotFound();
            //}


            //    .Include(u => u.FirstName)
            //    .Include(u => u.LastName)
            //    .Include(u => u.UserName)
            //    .Include(u => u.Email)
            //    .Include(u => u.PhoneNumber)
            //    .Where(u => u.Id == id)
            //    .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            //return View(user);
            return PartialView("Details",user);
        }

        // GET: Members/MembersDetails/5
        public async Task<IActionResult> MembersDetails(string id)
        {
            var user = await _context.Users
            .Where(u => u.Id == id)
            .FirstOrDefaultAsync();
            //if (id == null)
            //{
            //    return NotFound();
            //}

            //string userId = _userManager.GetUserId(User);
            //ViewData["CurrentUserId"] = userId;

            //var user = await _context.Users
            //    .Include(u => u.FirstName)
            //    .Include(u => u.LastName)
            //    .Include(u => u.UserName)
            //    .Include(u => u.Email)
            //    .Include(u => u.PhoneNumber)           
            //    .ThenInclude(h => h.User)
            //.Include(t => t.Comments)
            //    .ThenInclude(c => c.User)
            //.Include(t => t.Comments)
            //    .ThenInclude(c => c.Ticket)
            //.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            ViewBag.returnUrl = Request.Headers["Referer"].ToString();
            return View(user);
        }




        // GET: Members/Create
        public async Task<IActionResult> Create(int? projId)
        {

            // Get Current User
            BTUser btUser = await _userManager.GetUserAsync(User);

            // Get Current User Company Id
            int companyId = User.Identity.GetCompanyId().Value;

            Ticket ticket = new();

            if (projId == null)
            {
                if (User.IsInRole("Admin"))
                {
                    ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjectsByCompany(companyId), "Id", "Name");
                }
                else
                {
                    ViewData["ProjectId"] = new SelectList(await _projectService.ListUserProjectsAsync(btUser.Id), "Id", "Name");
                }
            }
            else
            {
                ticket.ProjectId = (int)projId;
            }

            ViewBag.returnUrl = Request.Headers["Referer"].ToString();
            ViewData["TicketPriorityId"] = new SelectList(_context.Set<TicketPriority>(), "Id", "Name");
            ViewData["TicketTypeId"] = new SelectList(_context.Set<TicketType>(), "Id", "Name");

            return View(ticket);
        }


        public async Task <IActionResult> ChangePassword(string id)
        {
            ChangePasswordModel changePasswordModel = new();
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (user.Password != null)
            {

                //decoded base64string
                var encoder = new System.Text.UTF8Encoding();
                System.Text.Decoder utf8Decode = encoder.GetDecoder();
                byte[] todecodeByte = Convert.FromBase64String(user.Password);
                int charCount = utf8Decode.GetCharCount(todecodeByte, 0, todecodeByte.Length);
                char[] decodedChar = new char[charCount];
                utf8Decode.GetChars(todecodeByte, 0, todecodeByte.Length, decodedChar, 0);
                string result = new String(decodedChar);
                user.Password = result;
            }


            changePasswordModel.OldPassword= user.Password;





            ViewBag.returnUrl = Request.Headers["Referer"].ToString();
            //return View(changePasswordModel);
            return PartialView(changePasswordModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string id, string returnUrl, ChangePasswordModel inviteView)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FindAsync(id);

                    if (existingUser == null)
                    {
                        return NotFound();
                    }


                    if (existingUser.Password != null)
                    {

                        //decoded base64string
                        var encoder = new System.Text.UTF8Encoding();
                        System.Text.Decoder utf8Decode = encoder.GetDecoder();
                        byte[] todecodeByte = Convert.FromBase64String(existingUser.Password);
                        int charCount = utf8Decode.GetCharCount(todecodeByte, 0, todecodeByte.Length);
                        char[] decodedChar = new char[charCount];
                        utf8Decode.GetChars(todecodeByte, 0, todecodeByte.Length, decodedChar, 0);
                        string oldPasswordResult = new String(decodedChar);
                        existingUser.Password = oldPasswordResult;
                    }

                   

                    if (existingUser.Password != inviteView.Password)
                    {
                        var result = await _userManager.ChangePasswordAsync(existingUser, existingUser.Password, inviteView.Password);

                        if (result.Succeeded)
                        {

                            byte[] encData_byte = new byte[inviteView.Password.Length];
                            encData_byte = System.Text.Encoding.UTF8.GetBytes(inviteView.Password);
                            string encodedData = Convert.ToBase64String(encData_byte);

                            existingUser.Password = encodedData;
                            //await _userManager.AddToRoleAsync(user, Roles.Developer.ToString());
                            //await _signInManager.RefreshSignInAsync(existingUser);
                            _context.Update(existingUser);
                            _context.SaveChanges();
                        }
                    }
                  
                    // Update the properties of the existingUser entity.
                    //existingUser.FirstName = user.FirstName;
                    //existingUser.LastName = user.LastName;
                    //existingUser.UserName = user.UserName;
                    //existingUser.PhoneNumber = user.PhoneNumber;
                    //existingUser.Email = user.Email;

                    // Save changes to the database.
                    await _context.SaveChangesAsync();

                    // Redirect to the referring page or a success page.
                    return Redirect(returnUrl);
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Handle concurrency conflicts if needed.
                    // For example, you can show an error message or return a specific view.
                    ModelState.AddModelError("", "Concurrency conflict occurred.");
                }
            }

            return View();
        }


        // GET: Members/Edit/
        // HIDE the Developer
        public async Task<IActionResult> Edit(string id)
        {

            var user = await _context.Users.FindAsync(id);

            MemberEditViewModel model = new();

            if (User.IsInRole("Admin"))
            {
                int companyId = User.Identity.GetCompanyId().Value;
              
                
                var shifts = await _context.RotationShift.Where(x => x.CompanyId == companyId).ToListAsync();
                model.ShiftList = new SelectList(shifts, "Id", "ShiftInterval");
                model.ShiftId=Convert.ToInt32(user.shift);
                model.FirstName=user.FirstName;
                model.LastName=user.LastName;
                model.Email=user.Email;
                model.UserName=user.UserName;
                model.PhoneNumber=user.PhoneNumber;
                model.Id = id;
            }








          
            if (user == null)
            {
                return NotFound();
            }

            if (user.Password != null)
            {

                //decoded base64string
                var encoder = new System.Text.UTF8Encoding();
                System.Text.Decoder utf8Decode = encoder.GetDecoder();
                byte[] todecodeByte = Convert.FromBase64String(user.Password);
                int charCount = utf8Decode.GetCharCount(todecodeByte, 0, todecodeByte.Length);
                char[] decodedChar = new char[charCount];
                utf8Decode.GetChars(todecodeByte, 0, todecodeByte.Length, decodedChar, 0);
                string result = new String(decodedChar);
                model.Password = result;
            }


       
            // Return to Referring Page
            ViewBag.returnUrl = Request.Headers["Referer"].ToString();


            //ViewData["Id"] = new SelectList(_context.Users, user.Id);
            //ViewData["FirstName"] =  user.FirstName;
            //ViewData["LastName"] = new SelectList(_context.Users, user.LastName);
            //ViewData["Username"] = new SelectList(_context.Users, user.UserName);
            //ViewData["Email"] = new SelectList(_context.Users, user.Email);
            //ViewData["PhoneNumber"] = new SelectList(_context.Users, user.PhoneNumber);


            // BTUser
            //BTUser users = await _userManager.GetUserAsync(User);

            //// UserId
            //string Id = _userManager.GetUserId(User);

            //int companyId = User.Identity.GetCompanyId().Value;
            //List<BTUser> members = await _infoService.GetAllMembersAsync(companyId, id);

            //return View(user);
            return PartialView("Edit", model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string returnUrl, MemberEditViewModel inviteUser)
        {


            {
                if (id != inviteUser.Id)
                {
                    return NotFound();
                }


                if (ModelState.IsValid)
                {
                    try
                    {
                        var existingUser = await _context.Users.FindAsync(id);

                        if (existingUser == null)
                        {
                            return NotFound();
                        }


                        if (existingUser.Password != null)
                        {

                            //decoded base64string
                            var encoder = new System.Text.UTF8Encoding();
                            System.Text.Decoder utf8Decode = encoder.GetDecoder();
                            byte[] todecodeByte = Convert.FromBase64String(existingUser.Password);
                            int charCount = utf8Decode.GetCharCount(todecodeByte, 0, todecodeByte.Length);
                            char[] decodedChar = new char[charCount];
                            utf8Decode.GetChars(todecodeByte, 0, todecodeByte.Length, decodedChar, 0);
                            string oldPasswordResult = new String(decodedChar);
                            existingUser.Password = oldPasswordResult;
                        }

                        existingUser.FirstName = inviteUser.FirstName;
                        existingUser.LastName = inviteUser.LastName;
                        existingUser.UserName = inviteUser.UserName;
                        existingUser.PhoneNumber = inviteUser.PhoneNumber;
                        existingUser.Email = inviteUser.Email;
                        existingUser.shift = inviteUser.ShiftId.ToString();

                        if (existingUser.Password != inviteUser.Password)
                        {
                            var result = await _userManager.ChangePasswordAsync(existingUser, existingUser.Password, inviteUser.Password);

                            if (result.Succeeded)
                            {

                                byte[] encData_byte = new byte[inviteUser.Password.Length];
                                encData_byte = System.Text.Encoding.UTF8.GetBytes(inviteUser.Password);
                                string encodedData = Convert.ToBase64String(encData_byte);

                                existingUser.Password = encodedData;
                                //await _userManager.AddToRoleAsync(user, Roles.Developer.ToString());
                                //await _signInManager.RefreshSignInAsync(existingUser);
                                _context.Update(existingUser);
                                _context.SaveChanges();
                            }
                        }
                        else
                        {
                            byte[] encData_byte = new byte[existingUser.Password.Length];
                            encData_byte = System.Text.Encoding.UTF8.GetBytes(inviteUser.Password);
                            string encodedData = Convert.ToBase64String(encData_byte);

                            existingUser.Password = encodedData;
                        }
                        // Update the properties of the existingUser entity.
                        //existingUser.FirstName = user.FirstName;
                        //existingUser.LastName = user.LastName;
                        //existingUser.UserName = user.UserName;
                        //existingUser.PhoneNumber = user.PhoneNumber;
                        //existingUser.Email = user.Email;

                        // Save changes to the database.
                        await _context.SaveChangesAsync();

                        // Redirect to the referring page or a success page.
                        return Redirect(returnUrl);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        // Handle concurrency conflicts if needed.
                        // For example, you can show an error message or return a specific view.
                        ModelState.AddModelError("", "Concurrency conflict occurred.");
                    }
                }

                // If ModelState is not valid, return to the edit view with validation errors.
                return View(inviteUser);
            }
        }



        private bool TicketExists(object id)
        {
            throw new NotImplementedException();
        }

        // GET: Members/Delete/
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Return to Referring Page
            ViewBag.returnUrl = Request.Headers["Referer"].ToString();
            BTUser btUser = await _userManager.GetUserAsync(User);

            #region CheckDemoUser
            // !!! Demo User should not be allowed to delete
            var isDemo = await _roleService.IsUserInRoleAsync(btUser, Roles.DemoUser.ToString());

            if (isDemo)
            {
                TempData["StatusMessage"] = "Error - You do not have access to complete this action.";
                return Redirect(ViewBag.returnUrl);
            }
            #endregion
            var user = await _context.Users
           .Where(u => u.Id == id)
           .FirstOrDefaultAsync();
            //var users = await _context.Users
            //    .Include(t => t.FirstName)
            //    .Include(t => t.LastName)
            //    .Include(t => t.UserName)
            //    .Include(t => t.Email)
            //    .Include(t => t.PhoneNumber)
            //    .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Members/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string? id, string returnUrl)
        {
            BTUser btUser = await _userManager.GetUserAsync(User);

            #region CheckDemoUser
            // !!! Demo User should not be allowed to delete
            var isDemo = await _roleService.IsUserInRoleAsync(btUser, Roles.DemoUser.ToString());

            if (isDemo)
            {
                TempData["StatusMessage"] = "Error - You do not have access to complete this action.";
                return Redirect(returnUrl);
            }
            #endregion
            List<TicketComment> userTicketComment = new();
            userTicketComment = await _context.TicketComment.Where(p => p.UserId == id).ToListAsync();
            //var user1 = await _context.TicketComment.FirstAsync(id);

            foreach (var ticketComment in userTicketComment)
            {
                _context.TicketComment.Remove(ticketComment);
                await _context.SaveChangesAsync();
            }

            List<TicketHistory> userTicketHistory = new();
            userTicketHistory = await _context.TicketHistory.Where(p => p.UserId == id).ToListAsync();


            foreach (var tickethistory in userTicketHistory)
            {
                _context.TicketHistory.Remove(tickethistory);
                await _context.SaveChangesAsync();
            }

            List<Ticket> userTicket = new();
            userTicket = await _context.Ticket.Where(p => p.DeveloperUserId == id).ToListAsync();


            foreach (var ticket in userTicket)
            {
                _context.Ticket.Remove(ticket);
                await _context.SaveChangesAsync();
            }

            List<Ticket> userTicket1 = new();
            userTicket1 = await _context.Ticket.Where(p => p.OwnerUserId == id).ToListAsync();


            foreach (var ticket in userTicket1)
            {
                _context.Ticket.Remove(ticket);
                await _context.SaveChangesAsync();
            }


            var user = await _context.Users.FindAsync(id);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            //return RedirectToAction(nameof(Index));
            return Redirect(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deletes(string id)
        {

            BTUser btUser = await _userManager.GetUserAsync(User);

            #region CheckDemoUser
            // !!! Demo User should not be allowed to delete
            var isDemo = await _roleService.IsUserInRoleAsync(btUser, Roles.DemoUser.ToString());
            var returnUrl = Request.Headers["Referer"].ToString();
            if (isDemo)
            {
                TempData["StatusMessage"] = "Error - You do not have access to complete this action.";
                return Redirect(returnUrl);
            }
            #endregion
            List<TicketComment> userTicketComment = new();
            userTicketComment = await _context.TicketComment.Where(p => p.UserId == id).ToListAsync();
            //var user1 = await _context.TicketComment.FirstAsync(id);

            foreach (var ticketComment in userTicketComment)
            {
                _context.TicketComment.Remove(ticketComment);
                await _context.SaveChangesAsync();
            }

            List<TicketHistory> userTicketHistory = new();
            userTicketHistory = await _context.TicketHistory.Where(p => p.UserId == id).ToListAsync();


            foreach (var tickethistory in userTicketHistory)
            {
                _context.TicketHistory.Remove(tickethistory);
                await _context.SaveChangesAsync();
            }

            List<Ticket> userTicket = new();
            userTicket = await _context.Ticket.Where(p => p.DeveloperUserId == id).ToListAsync();


            foreach (var ticket in userTicket)
            {
                _context.Ticket.Remove(ticket);
                await _context.SaveChangesAsync();
            }

            List<Ticket> userTicket1 = new();
            userTicket1 = await _context.Ticket.Where(p => p.OwnerUserId == id).ToListAsync();


            foreach (var ticket in userTicket1)
            {
                _context.Ticket.Remove(ticket);
                await _context.SaveChangesAsync();
            }


            var user = await _context.Users.FindAsync(id);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            //return RedirectToAction(nameof(Index));
            return Redirect(returnUrl);
        }
    }
}
