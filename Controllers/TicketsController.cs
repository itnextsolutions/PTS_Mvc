using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVC_BugTracker.Data;
using MVC_BugTracker.Extensions;
using MVC_BugTracker.Models;
using MVC_BugTracker.Models.Enums;
using MVC_BugTracker.Models.ViewModels;
using MVC_BugTracker.Services.Interfaces;

namespace MVC_BugTracker.Controllers
{
    [Authorize]
    public class TicketsController : Controller
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


        public TicketsController(ApplicationDbContext context,
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

        // GET: Tickets
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Ticket
                                        .Include(t => t.DeveloperUser)
                                        .Include(t => t.OwnerUser)
                                        .Include(t => t.TicketPriority)
                                        .Include(t => t.Project)
                                        .Include(t => t.TicketStatus)
                                        .ToListAsync();

            return View(await applicationDbContext);
        }

        // GET: ALL Tickets
        public async Task<IActionResult> AllTickets()
        {
            // GET company id
            int companyId = User.Identity.GetCompanyId().Value;

            List<Ticket> tickets = new();

            try
            {
                tickets = await _infoService.GetAllTicketsAsync(companyId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"*** ERROR *** - Error getting all tickets - {ex.Message}");
                throw;
            }

            return View(tickets);
        }

        // GET: MY Tickets
        public async Task<IActionResult> MyTickets()
        {
            // GET my user id
            string userId = _userManager.GetUserId(User);

            // SET ViewModel
            MyTicketsViewModel tickets = new();
            List<Ticket> developerTickets = new();
            List<Ticket> submitterTickets = new();

            try
            {
                developerTickets = await _ticketService.GetAllTicketsByRoleAsync(Roles.Developer.ToString(), userId);
                tickets.Developer = developerTickets;

                submitterTickets = await _ticketService.GetAllTicketsByRoleAsync(Roles.Submitter.ToString(), userId);
                tickets.Submitter = submitterTickets;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"*** ERROR *** - Error getting all tickets - {ex.Message}");
                throw;
            }

            return View(tickets);
        }

        // GET: Tickets/Details/5
        //[Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Details(int? id)
        {
            TicketTaskViewModel model = new();

            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Ticket
                .Include(t => t.DeveloperUser)
                .Include(t => t.OwnerUser)
                .Include(t => t.TicketPriority)
                .Include(t => t.Project)
                .Include(t => t.TicketStatus)
                .FirstOrDefaultAsync(m => m.Id == id);

            List<TicketTask> ticketTask = new();


            ticketTask = await _infoService.GetAllTicketTask(id);
 
            model.TaskViewModels= ticketTask;

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // GET: Tickets/TicketDetails/5
        public async Task<IActionResult> TicketDetails(int? id)
        {

            TicketTaskViewModel model = new();

            if (id == null)
            {
                return NotFound();
            }

            string userId = _userManager.GetUserId(User);
            ViewData["CurrentUserId"] = userId;

            var ticket = await _context.Ticket
                .Include(t => t.DeveloperUser)
                .Include(t => t.OwnerUser)
                .Include(t => t.TicketPriority)
                .Include(t => t.Project)
                .Include(t => t.TicketStatus)
                .Include(t => t.TicketType)
                .Include(t => t.Attachments)
                .Include(t => t.History)
                    .ThenInclude(h => h.User)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.Ticket)
                .FirstOrDefaultAsync(m => m.Id == id);

            List<TicketTask> ticketTask = new();


            ticketTask = await _infoService.GetAllTicketTask(id);
            if(ticketTask != null)
            {
                model.TaskViewModels = ticketTask;
            }
           

            model.tic = ticket;

            if (ticket == null && ticketTask==null)
            {
                return NotFound();
            }

            ViewBag.returnUrl = Request.Headers["Referer"].ToString();
            return View(model);
        }

        // GET: Tickets/Create

        [Route("Tickets/Create/{projId?}")]
        [HttpGet()]
        public async Task<IActionResult> Create(int? projId)
        {
            // REMOVE
            //ViewData["DeveloperUserId"] = new SelectList(_context.Users, "Id", "Id");
            //ViewData["OwnerUserId"] = new SelectList(_context.Users, "Id", "Id");
            //ViewData["TicketStatusId"] = new SelectList(_context.Set<TicketStatus>(), "Id", "Id");
            //ViewData["ProjectId"] = new SelectList(_context.Project, "Id", "Name");

           

            // Get Current User
            BTUser btUser = await _userManager.GetUserAsync(User);

            // Get Current User Company Id
            int companyId = User.Identity.GetCompanyId().Value;

            //Ticket ticket = new();

            //TicketTaskViewModel viewmodel1 = new();

            var viewmodel = new TicketTaskViewModel
            {
                TaskViewModels = new List<TicketTask> { new TicketTask { } } // Start with one empty row
            };


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
                viewmodel.tic.ProjectId = (int)projId;
            }

            ViewBag.returnUrl = Request.Headers["Referer"].ToString();
            ViewData["TicketPriorityId"] = new SelectList(_context.Set<TicketPriority>(), "Id", "Name");
            ViewData["TicketTypeId"] = new SelectList(_context.Set<TicketType>(), "Id", "Name");

            //return View(viewmodel);
            return PartialView("Create",viewmodel);
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string returnUrl,  TicketTaskViewModel viewmodel)
        {
            // Created,Updated,Archived,ArchivedDate,OwnerUserId,DeveloperUserId,TicketStatusId,

            if (ModelState.IsValid)
            {
                BTUser btUser = await _userManager.GetUserAsync(User);

                viewmodel.tic.Created = DateTimeOffset.Now;

                string userId = _userManager.GetUserId(User);
                viewmodel.tic.OwnerUserId = userId;

                // All new tickets are 'New'
                viewmodel.tic.TicketStatusId = (await _ticketService.LookupTicketStatusIdAsync("New")).Value;

                Ticket model = new()
                {
                    ProjectId=viewmodel.tic.ProjectId,
                    Title=viewmodel.tic.Title,
                    Description=viewmodel.tic.Description,
                    TicketPriorityId=viewmodel.tic.TicketPriorityId,
                    TicketTypeId=viewmodel.tic.TicketTypeId,
                    StartDate=viewmodel.tic.StartDate,
                    DueDate=viewmodel.tic.DueDate,
                    TicketStatusId=viewmodel.tic.TicketStatusId

                };


                await _context.AddAsync(model);
                await _context.SaveChangesAsync();
               
                if (viewmodel.TaskViewModels != null)
                {
                    foreach (var i in viewmodel.TaskViewModels)
                    {
                        TicketTask modelTicketTask = new()
                        {
                            TaskDescription = i.TaskDescription,
                            TaskTitle = i.TaskTitle,
                            TicketId = model.Id
                        };
                        await _context.AddAsync(modelTicketTask);
                        await _context.SaveChangesAsync();
                    }
                }

                #region Add History
                // Add History
                Ticket newTicket = await _context.Ticket
                                                 .Include(t => t.TicketPriority)
                                                 .Include(t => t.TicketStatus)
                                                 .Include(t => t.TicketType)
                                                 .Include(t => t.Project)
                                                 .Include(t => t.DeveloperUser)
                                                 .AsNoTracking().FirstOrDefaultAsync(t => t.Id == model.Id);

                await _historyService.AddHistoryAsync(null, newTicket, btUser.Id);
                #endregion

                #region Add Notification
                BTUser projectManager = await _projectService.GetProjectManagerAsync(viewmodel.tic.ProjectId);
                int companyId = User.Identity.GetCompanyId().Value;

                Notification notification = new()
                {
                    TicketId = model.Id,
                    Title = "New Ticket",
                    Message = $"New Ticket: {viewmodel.tic.Title}, was created by {btUser?.FullName}",
                    Created = DateTimeOffset.Now,
                    SenderId = btUser?.Id,
                    RecipientId = projectManager?.Id
                };

                // Note - PM will never be null; check the firstName
                if(projectManager.FirstName != null)
                {
                    // Notify the PM
                    await _notificationService.SaveNotificationAsync(notification);
                    await _notificationService.EmailNotificationAsync(notification, notification.Title);
                } else
                {
                    // Notify the Admin
                    await _notificationService.AdminsNotificationAsync(notification, companyId);
                    await _notificationService.EmailNotificationAsync(notification, notification.Title);
                }

                #endregion


                //return RedirectToAction("Details", "Tickets", new { id = ticket.Id });
                //return Redirect(returnUrl);
                return Json(new { success = true, message = "Ticket saved successfully!", url = returnUrl });
                //return View();
            }

        
            //return View(ticket);
            //return RedirectToAction("AllTickets");
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                               .Select(e => e.ErrorMessage)
                               .ToList();
            // return Json(new { success = false, errors = errors });
            return Json(new { success = false, errors = "Please Fill All Required Fields!" });
        }

        // Assign Ticket
        [HttpGet]
        public async Task<IActionResult> AssignTicket(int? ticketId){

            if (!ticketId.HasValue)
            {
                return NotFound();
            }
            
            
            // Company Id
            int companyId = User.Identity.GetCompanyId().Value;
            
            AssignDeveloperViewModel model = new();

            model.Ticket = (await _ticketService.GetAllTicketsByCompanyAsync(companyId)).FirstOrDefault(t => t.Id == ticketId);
            model.Developers = new SelectList(await _projectService.DevelopersOnProjectAsync(model.Ticket.ProjectId), "Id", "FullName");


            return View(model);
        }

        // Assign Ticket: POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTicket(AssignDeveloperViewModel viewModel)
        {
            if (!string.IsNullOrEmpty(viewModel.DeveloperId))
            {
                int companyId = User.Identity.GetCompanyId().Value;

                BTUser btUser = await _userManager.GetUserAsync(User);
                BTUser developer = (await _companyService.GetAllMembersAsync(companyId)).FirstOrDefault(m => m.Id == viewModel.DeveloperId);
                BTUser projectManager = await _projectService.GetProjectManagerAsync(viewModel.Ticket.ProjectId);

                Ticket oldTicket = await _context.Ticket
                    .Include(t => t.TicketPriority)
                    .Include(t => t.TicketStatus)
                    .Include(t => t.TicketType)
                    .Include(t => t.Project)
                    .Include(t => t.DeveloperUser)
                    .AsNoTracking().FirstOrDefaultAsync(t => t.Id == viewModel.Ticket.Id);

                await _ticketService.AssignTicketAsync(viewModel.Ticket.Id, viewModel.DeveloperId);


                Ticket newTicket = await _context.Ticket
                    .Include(t => t.TicketPriority)
                    .Include(t => t.TicketStatus)
                    .Include(t => t.TicketType)
                    .Include(t => t.Project)
                    .Include(t => t.DeveloperUser)
                    .AsNoTracking().FirstOrDefaultAsync(t => t.Id == viewModel.Ticket.Id);

                await _historyService.AddHistoryAsync(oldTicket, newTicket, btUser.Id);
            }
            return RedirectToAction("Details", new { id = viewModel.Ticket.Id });
        }



        // GET: Tickets/Edit/5
        // HIDE the Developer
        [Route("Tickets/Edit/{id?}")]
        [HttpGet()]
        public async Task<IActionResult> Edit(int? id)
        {
            TicketTaskViewModel model = new();
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Ticket.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            model.tic = ticket;

            // Return to Referring Page
            ViewBag.returnUrl = Request.Headers["Referer"].ToString();

            #region IsThisMyTicket
            // Is this my ticket???
            bool isThisMyTicket = false;

            // BTUser
            BTUser btUser = await _userManager.GetUserAsync(User);

            // UserId
            string userId = _userManager.GetUserId(User);

            // Developer
            bool isDeveloper = false;
            if(ticket?.DeveloperUserId != null)
            {
                isDeveloper = (bool)(ticket?.DeveloperUserId.Equals(userId));
            }

            // Submitter
            bool isSubmitter = false;

            if(ticket?.OwnerUserId != null)
            {
                isSubmitter = (bool)(ticket?.OwnerUserId.Equals(userId));
            }

            // Project Manager
            var ticketPMId = (await _projectService.GetProjectManagerAsync(ticket.ProjectId)).Id;
            bool isPM = false;
            
            if(ticketPMId != null)
            {
                isPM = ticketPMId.Equals(userId);
            }

            // Admin
            var isAdmin = await _roleService.IsUserInRoleAsync(btUser, Roles.Admin.ToString());

            // !!! Demo User should not be allowed to edit
            var isDemo = await _roleService.IsUserInRoleAsync(btUser, Roles.DemoUser.ToString());

            isThisMyTicket = (isDeveloper || isSubmitter || isPM || isAdmin) && (!isDemo);


            if (!isThisMyTicket)
            {
                TempData["StatusMessage"] = "Error - You do not have access to complete this action.";
                return Redirect(ViewBag.returnUrl);
            }
            #endregion

            List<TicketTask> ticketTask = new();


            ticketTask = await _infoService.GetAllTicketTask(id);
            if (ticketTask != null)
            {
                model.TaskViewModels = ticketTask;
            }


            ViewData["ProjectName"] = (await _context.Ticket.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id)).Project.Name;
            ViewData["ProjectId"] = new SelectList(_context.Project, "Id", "Name", ticket.ProjectId);
            ViewData["TicketPriorityId"] = new SelectList(_context.Set<TicketPriority>(), "Id", "Name", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(_context.Set<TicketStatus>(), "Id", "Name", ticket.TicketStatusId);
            ViewData["TicketTypeId"] = new SelectList(_context.Set<TicketType>(), "Id", "Name", ticket.TicketTypeId);

            // Only company members (including PM)
            int companyId = User.Identity.GetCompanyId().Value;
            List<BTUser> members = await _infoService.GetAllMembersAsync(companyId);
            
            // Only Company Members
            ViewData["OwnerUserId"] = new SelectList(members, "Id", "FullName", ticket.OwnerUserId);
            
            // Only developers on the project
            List<BTUser> projectUsers = await _projectService.GetMembersWithoutPMAsync(ticket.ProjectId);

            //ViewData["DeveloperUserId"] = new SelectList(_context.Users, "Id", "FullName", ticket.DeveloperUserId);
            ViewData["DeveloperUserId"] = new SelectList(projectUsers, "Id", "FullName", ticket.DeveloperUserId);


            //return View(model);
            return PartialView("Edit", model);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string returnUrl, TicketTaskViewModel viewmodel,int taskCountEdit)
        {
            if (id != viewmodel.tic.Id)
            {
                return NotFound();
            }

            #region IsThisMyTicket
            // Is this my ticket???
            bool isThisMyTicket = false;

            List<TicketTask> oldticketTask = new();
            oldticketTask = await _infoService.GetAllTicketTask(id);
            

            //List<TicketTask> ticketTaskListOld = new();
            //for (int i = 0; i < taskCount; i++)
            //{
            //    var taskViewModeld = viewmodel.TaskViewModels[i];

               
            //    if(taskViewModeld.TicketTaskId!=0 && taskViewModeld.TicketTaskId != null)
            //    {
            //        ticketTaskListOld.Add(taskViewModeld);
            //    }
            //}


            // BTUser
            BTUser btUser = await _userManager.GetUserAsync(User);

            // UserId
            string userId = _userManager.GetUserId(User);

            // Developer
            bool isDeveloper = false;
            if (viewmodel.tic?.DeveloperUserId != null)
            {
                isDeveloper = (bool)(viewmodel.tic?.DeveloperUserId.Equals(userId));
            }

            // Submitter
            bool isSubmitter = false;

            if (viewmodel.tic?.OwnerUserId != null)
            {
                isSubmitter = (bool)(viewmodel.tic?.OwnerUserId.Equals(userId));
            }

            // Project Manager
            var ticketPMId = (await _projectService.GetProjectManagerAsync(viewmodel.tic.ProjectId)).Id;
            bool isPM = false;

            if (ticketPMId != null)
            {
                isPM = ticketPMId.Equals(userId);
            }

            // Admin
            var isAdmin = await _roleService.IsUserInRoleAsync(btUser, Roles.Admin.ToString());

            // !!! Demo User should not be allowed to edit
            var isDemo = await _roleService.IsUserInRoleAsync(btUser, Roles.DemoUser.ToString());

            isThisMyTicket = (isDeveloper || isSubmitter || isPM || isAdmin) && (!isDemo);


            if (!isThisMyTicket)
            {
                TempData["StatusMessage"] = "Error - You do not have access to complete this action.";
                return Redirect(returnUrl);
            }
            #endregion

            if (ModelState.IsValid)
            {

               

                List<TicketTask> ticketTaskListOld = new();
                for (int i = 0; i < taskCountEdit; i++)
                {
                    var taskViewModeld = viewmodel.TaskViewModels[i];


                    if (taskViewModeld.TicketTaskId != 0 && taskViewModeld.TicketTaskId != null)
                    {
                        ticketTaskListOld.Add(taskViewModeld);
                    }
                }



                





                Notification notification;
                
                // Get Current User
                btUser = await _userManager.GetUserAsync(User);

                // Get Current User Company Id
                int companyId = User.Identity.GetCompanyId().Value;

                // project manager
                BTUser projectManager = await _projectService.GetProjectManagerAsync(viewmodel.tic.ProjectId);

                // Old Ticket (AsNoTracking gives a snapshot)
                Ticket oldTicket = await _context.Ticket
                                             .Include(t => t.TicketPriority)
                                             .Include(t => t.TicketStatus)
                                             .Include(t => t.TicketType)
                                             .Include(t => t.Project)
                                             .Include(t => t.DeveloperUser)
                                             .AsNoTracking().FirstOrDefaultAsync(t => t.Id == viewmodel.tic.Id);

                try
                {
                    // Developer Check - Logical status change based on availability of developer
                    if(viewmodel.tic.DeveloperUserId == null)
                    {
                        viewmodel.tic.TicketStatusId = 5;
                    }

                    if(viewmodel.tic.DeveloperUserId != null && (viewmodel.tic.TicketStatusId == 5 || viewmodel.tic.TicketStatusId == 6))
                    {
                        viewmodel.tic.TicketStatusId = 4;
                    }


                    viewmodel.tic.Updated = DateTimeOffset.Now;
                    
                    _context.Update(viewmodel.tic);
                    await _context.SaveChangesAsync();

                  
                    if (ticketTaskListOld.Count > 0)
                    {
                        foreach (var taskViewModelold in ticketTaskListOld)
                        {
                            if (taskViewModelold.TicketTaskId != 0 && taskViewModelold.TicketTaskId != null)
                            {



                                TicketTask existingTask = await _context.TicketTask

                                                 .AsNoTracking().FirstOrDefaultAsync(t => t.TicketTaskId == taskViewModelold.TicketTaskId);

                                if (existingTask.TaskDescription != taskViewModelold.TaskDescription || existingTask.TaskTitle != taskViewModelold.TaskTitle)
                                {

                                    //var existingTask = await _infoService.GetTicketTaskbyz(taskViewModel.TicketTaskId); // Replace with your actual method to get the task
                                    if (existingTask != null)
                                    {
                                        // Update properties
                                        existingTask.TaskTitle = taskViewModelold.TaskTitle;
                                        existingTask.TaskDescription = taskViewModelold.TaskDescription;
                                        existingTask.TicketId = viewmodel.tic.Id;
                                        // Update the task in the database
                                        // Replace with your actual update method
                                        await _infoService.UpdateTicketTask(existingTask);

                                    }

                                }
                            }
                        }
                    }
                    else
                    {
                        if (oldticketTask.Count > 0)
                        {
                            foreach (var task in oldticketTask)
                            {
                                if (task != null)
                                {
                                    _context.TicketTask.Remove(task);
                                }
                            }
                        }


                        if (viewmodel.TaskViewModels != null && viewmodel.TaskViewModels.Count > 0)
                        {
                            foreach (var taskViewModel in viewmodel.TaskViewModels)
                            {
                                var newTask = new TicketTask
                                {
                                    TaskTitle = taskViewModel.TaskTitle,
                                    TaskDescription = taskViewModel.TaskDescription,
                                    TicketId = viewmodel.tic.Id,
                                    // Set other properties as needed
                                };

                                // Create the new task in the database
                                _context.AddAsync(newTask); // Replace with your actual create method
                                await _context.SaveChangesAsync();


                            }
                        }
                    }

                    #region Notification
                    // Create and save notification
                    notification = new()
                    {
                        TicketId = viewmodel.tic.Id,
                        Title = $"Ticket modified on project - {oldTicket.Project.Name}",
                        Message = $"Ticket: {viewmodel.tic.Id}: {viewmodel.tic.Title} updated by {btUser.FullName}",
                        Created = DateTimeOffset.Now,
                        SenderId = btUser.Id,
                        RecipientId = projectManager?.Id
                    };

                    if (projectManager.FirstName != null)
                    {
                        // Notify the PM 
                        await _notificationService.SaveNotificationAsync(notification);
                        await _notificationService.EmailNotificationAsync(notification, notification.Title);
                    }
                    else
                    {
                        // Notify the Admin
                        await _notificationService.AdminsNotificationAsync(notification, companyId);
                        await _notificationService.EmailNotificationAsync(notification, notification.Title);
                    }

                    if(viewmodel.tic.DeveloperUserId != null)
                    {
                        // Alert developer if the ticket is modified
                        notification = new()
                        {
                            TicketId = viewmodel.tic.Id,
                            Title = "A ticket assigned to you has been modified",
                            Message = $"Ticket: {viewmodel.tic.Id}: {viewmodel.tic.Title} updated by {btUser.FullName}",
                            Created = DateTimeOffset.Now,
                            SenderId = btUser.Id,
                            RecipientId = viewmodel.tic.DeveloperUserId
                        };

                        await _notificationService.SaveNotificationAsync(notification);
                        await _notificationService.EmailNotificationAsync(notification, notification.Title);
                    }
                    #endregion

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(viewmodel.tic.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                // New Ticket (AsNoTracking gives a snapshot)
                Ticket newTicket = await _context.Ticket
                                             .Include(t => t.TicketPriority)
                                             .Include(t => t.TicketStatus)
                                             .Include(t => t.TicketType)
                                             .Include(t => t.Project)
                                             .Include(t => t.DeveloperUser)
                                             .AsNoTracking().FirstOrDefaultAsync(t => t.Id == viewmodel.tic.Id);

                await _historyService.AddHistoryAsync(oldTicket, newTicket, btUser.Id);
                await _context.SaveChangesAsync();
                // Redirect to Referrer
                //return RedirectToAction("AllTickets");
                //return Redirect(returnUrl);
                return Json(new { success = true, message = "Ticket Updated successfully!", url = returnUrl });



              
            }

         
            //return View(viewmodel);
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                    .Select(e => e.ErrorMessage)
                                    .ToList();
            return Json(new { success = false, errors = errors });
        }

        // GET: Tickets/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
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
            TicketTaskViewModel deleteTaskViewModel = new TicketTaskViewModel();
            var ticket = await _context.Ticket
                .Include(t => t.DeveloperUser)
                .Include(t => t.OwnerUser)
                .Include(t => t.TicketPriority)
                .Include(t => t.Project)
                .Include(t => t.TicketStatus)
                .FirstOrDefaultAsync(m => m.Id == id);
            deleteTaskViewModel.tic = ticket;

            List<TicketTask> oldticketTaskRemove = new();
            oldticketTaskRemove = await _infoService.GetAllTicketTask(id);

            deleteTaskViewModel.TaskViewModels = oldticketTaskRemove;

            if (ticket == null)
            {
                return NotFound();
            }

            return View(deleteTaskViewModel);
        }

        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id, string returnUrl)
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
            TicketTaskViewModel deleteTaskViewModel = new TicketTaskViewModel();
            var ticket = await _context.Ticket.FindAsync(id);
            _context.Ticket.Remove(ticket);

            List<TicketTask> oldticketTaskRemove = new();
            oldticketTaskRemove = await _infoService.GetAllTicketTask(id);

            foreach(var oldtickettaskremove in oldticketTaskRemove)
            {
                _context.Remove(oldtickettaskremove);
            }

            await _context.SaveChangesAsync();
            //return RedirectToAction(nameof(Index));
            return Redirect(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deletes(int id)
        {
          
         
            var returnUrl = Request.Headers["Referer"].ToString();

           



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
            TicketTaskViewModel deleteTaskViewModel = new TicketTaskViewModel();
            var ticket = await _context.Ticket.FindAsync(id);
            _context.Ticket.Remove(ticket);

            List<TicketTask> oldticketTaskRemove = new();
            oldticketTaskRemove = await _infoService.GetAllTicketTask(id);

            foreach (var oldtickettaskremove in oldticketTaskRemove)
            {
                _context.Remove(oldtickettaskremove);
            }

            await _context.SaveChangesAsync();
            //return RedirectToAction(nameof(Index));
            return Redirect(returnUrl);
        }




        private bool TicketExists(int id)
        {
            return _context.Ticket.Any(e => e.Id == id);
        }
    }
}
