﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using MVC_BugTracker.Data;
using MVC_BugTracker.Extensions;
using MVC_BugTracker.Models;
using MVC_BugTracker.Models.Enums;
using MVC_BugTracker.Models.ViewModels;
using MVC_BugTracker.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;

namespace MVC_BugTracker.Controllers
{
    [Authorize]
    [Authorize(Roles = "Admin")]
    public class InvitesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly IDataProtector _protector;
        private readonly IBTProjectService _projectService;
        private readonly IEmailSender _emailService;
        private readonly IBTInviteService _inviteService;
        private readonly ILogger<InviteViewModel> _logger;
        public InvitesController(ApplicationDbContext context,
                              UserManager<BTUser> userManager,
                              IDataProtectionProvider dataProtectionProvider,
                              IBTProjectService projectService,
                              IEmailSender emailService, ILogger<InviteViewModel> logger,
                              IBTInviteService inviteService)
        {
            _context = context;
            _userManager = userManager;
            _protector = dataProtectionProvider.CreateProtector("MVC.BugTracker.21");
            _projectService = projectService;
            _emailService = emailService;
            _inviteService = inviteService;

            _logger = logger;
        }

        // GET: Invites
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Invite.Include(i => i.Company).Include(i => i.Invitee).Include(i => i.Invitor).Include(i => i.Project);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Invites/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invite = await _context.Invite
                .Include(i => i.Company)
                .Include(i => i.Invitee)
                .Include(i => i.Invitor)
                .Include(i => i.Project)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (invite == null)
            {
                return NotFound();
            }

            return View(invite);
        }

        // GET: Invites/Create

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            InviteViewModel model = new();

            if (User.IsInRole("Admin"))
            {
                int companyId = User.Identity.GetCompanyId().Value;
                List<Project> projects = await _projectService.GetAllProjectsByCompany(companyId);

                model.ProjectsList = new SelectList(projects, "Id", "Name");
                model.Email = null;
                model.Password=null;
                var shifts = await _context.RotationShift.Where(x=>x.CompanyId==companyId).ToListAsync();
                model.ShiftList = new SelectList(shifts, "Id", "ShiftInterval");
            }
            //else if (User.IsInRole("ProjectManager"))
            //{
            //    string userId = _userManager.GetUserId(User);
            //    List<Project> projects = await _projectService.ListUserProjectsAsync(userId);
            //    model.ProjectsList = new SelectList(projects, "Id", "Name");
            //}

            return View(model);
        }

        // POST: Invites/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(InviteViewModel viewModel)
        {
            var companyId = User.Identity.GetCompanyId();

            Guid guid = Guid.NewGuid();

            var token = _protector.Protect(guid.ToString());
            var email = _protector.Protect(viewModel.Email);

            var callbackUrl = Url.Action("ProcessInvite", "Invites", new { token, email }, protocol: Request.Scheme);

            var body = "Please join my Company." + Environment.NewLine + "Please click the following link to join <a href=\"" + callbackUrl + "\">COLLABORATE</a>";
            var destination = viewModel.Email;
            var subject = "Company Invite";

            var returnUrl = Request.Headers["Referer"].ToString();
            await _emailService.SendEmailAsync(destination, subject, body);
            if (ModelState.IsValid)
            {
                //Create record in the Invites table
                Invite model = new()
                {
                    InviteeEmail = viewModel.Email,
                    InviteeFirstName = viewModel.FirstName,
                    InviteeLastName = viewModel.LastName,
                    CompanyToken = guid,
                    CompanyId = companyId.Value,
                    ProjectId = viewModel?.ProjectId,
                    InviteDate = DateTimeOffset.Now,
                    InvitorId = _userManager.GetUserId(User),
                    //DueDate = viewModel?.DueDate,
                    //StartDate = viewModel?.StartDate,
                    IsValid = true
                };



                _context.Invite.Add(model);
                _context.SaveChanges();

                //var shiftTimeStart = viewModel?.StartDate.Value.Hour;

                //var shiftTimeEnd = viewModel?.DueDate.Value.Hour;

                //var shiftTime = "";
                //if (shiftTimeStart > 5 && shiftTimeEnd < 15)
                //{
                //    shiftTime = "1st-shift";
                //}

                //if (shiftTimeStart > 13 && shiftTimeEnd < 23)
                //{
                //    shiftTime = "2nd-shift";
                //}

                //if (shiftTimeStart > 21 && shiftTimeEnd < 7)
                //{
                //    shiftTime = "3rd-shift";
                //}

                //base64Encode password

                byte[] encData_byte = new byte[viewModel.Password.Length];
                encData_byte = System.Text.Encoding.UTF8.GetBytes(viewModel.Password);
                string encodedData = Convert.ToBase64String(encData_byte);
                var shiftsv = _context.RotationShift.Where(x => x.Id == viewModel.ShiftId).ToList();
               
                var user = new BTUser
                {
                    CompanyId = companyId.Value,
                    UserName = viewModel.Email,
                    Email = viewModel.Email,
                    FirstName = viewModel.FirstName,
                    LastName = viewModel.LastName,
                    //shift = shiftTime,
                   shift=viewModel.ShiftId.ToString(),
                    EmailConfirmed = true,
                    Password= encodedData
                };
                //_context.Add(user);
                //_context.SaveChanges();
                var result = await _userManager.CreateAsync(user, viewModel.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                    //await _userManager.AddToRoleAsync(user, Roles.Developer.ToString());
                    _context.Update(user);
                    _context.SaveChanges();
                    //return RedirectToAction("Dashboard", "Home");

                }
               else
                {
                    //ModelState.AddModelError(string.Empty, error.Description);
                    //return View("Create");
                    var errorsResult = result.Errors.Select(e => e.Description)
                                  .ToList();
                    return Json(new { success = false, errors = errorsResult });
                }
                return Json(new { success = true, message = "Members have been successfully invited!"});



                //return RedirectToAction("Dashboard", "Home");
            }
            //return RedirectToAction("Create", "Invites");


            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                  .Select(e => e.ErrorMessage)
                                  .ToList();
            return Json(new { success = false, errors = errors });
        }


        [HttpGet]
        public async Task<IActionResult> ProcessInvite(string token, string email)
        {
            if (token == null)
            {
                return NotFound();
            }

            Guid companyToken = Guid.Parse(_protector.Unprotect(token));
            string inviteeEmail = _protector.Unprotect(email);

            //Use InviteService to validate invite code 
            Invite invite = await _inviteService.GetInviteAsync(companyToken, inviteeEmail);

            if (invite != null)
            {
                return View(invite);
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessInvite(Invite invite)
        {
            return RedirectToPage("RegisterByInvite", new { invite });
        }


        // GET: Invites/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invite = await _context.Invite.FindAsync(id);
            if (invite == null)
            {
                return NotFound();
            }
            ViewData["CompanyId"] = new SelectList(_context.Company, "Id", "Id", invite.CompanyId);
            ViewData["InviteeId"] = new SelectList(_context.Users, "Id", "Id", invite.InviteeId);
            ViewData["InvitorId"] = new SelectList(_context.Users, "Id", "Id", invite.InvitorId);
            ViewData["ProjectId"] = new SelectList(_context.Set<Project>(), "Id", "Id", invite.ProjectId);
            return View(invite);
        }

        // POST: Invites/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CompanyId,ProjectId,InviteeId,InvitorId,InviteDate,CompanyToken,InviteeEmail,FirstName,LastName,isValid")] Invite invite)
        {
            if (id != invite.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(invite);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InviteExists(invite.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CompanyId"] = new SelectList(_context.Company, "Id", "Id", invite.CompanyId);
            ViewData["InviteeId"] = new SelectList(_context.Users, "Id", "Id", invite.InviteeId);
            ViewData["InvitorId"] = new SelectList(_context.Users, "Id", "Id", invite.InvitorId);
            ViewData["ProjectId"] = new SelectList(_context.Set<Project>(), "Id", "Id", invite.ProjectId);
            return View(invite);
        }

        // GET: Invites/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invite = await _context.Invite
                .Include(i => i.Company)
                .Include(i => i.Invitee)
                .Include(i => i.Invitor)
                .Include(i => i.Project)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (invite == null)
            {
                return NotFound();
            }

            return View(invite);
        }

        // POST: Invites/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invite = await _context.Invite.FindAsync(id);
            _context.Invite.Remove(invite);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InviteExists(int id)
        {
            return _context.Invite.Any(e => e.Id == id);
        }
    }
}
