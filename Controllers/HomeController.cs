﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVC_BugTracker.Models;
using MVC_BugTracker.Models.ViewModels;
using MVC_BugTracker.Services.Interfaces;
using MVC_BugTracker.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace MVC_BugTracker.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IBTCompanyInfoService _infoService;
        private readonly IBTProjectService _projectService;
        private readonly IBTTicketService _ticketService;
        private readonly UserManager<BTUser> _userManager;

        public HomeController(ILogger<HomeController> logger,
                              IBTCompanyInfoService infoService,
                              IBTProjectService projectService, 
                              IBTTicketService ticketService, 
                              UserManager<BTUser> userManager)
        {
            _logger = logger;
            _infoService = infoService;
            _projectService = projectService;
            _ticketService = ticketService;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            // return View();
            return RedirectToAction("Dashboard");
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            DashboardViewModel dashboardVM = new();

            int companyId = User.Identity.GetCompanyId().Value;
            BTUser btUser = await _userManager.GetUserAsync(User);

            Company company = await _infoService.GetCompanyInfoByIdAsync(companyId);

            List<Project> projects = await _projectService.GetAllProjectsByCompany(companyId);
            List<Ticket> tickets = await _ticketService.GetAllTicketsByCompanyAsync(companyId);
            List<BTUser> members = await _infoService.GetAllMembersAsync(companyId);

            dashboardVM.Company = company;
            dashboardVM.Projects = projects;
            dashboardVM.Tickets = tickets;
            dashboardVM.Members = members;

            return View(dashboardVM);
        }

        [HttpPost]
        public async Task<JsonResult> PieChartMethod()
        {
            int companyId = User.Identity.GetCompanyId().Value;

            List<Project> projects = await _projectService.GetAllProjectsByCompany(companyId);

            List<object> chartData = new();
            // chartData.Add(new object[] { "ProjectPriority", "TicketCount"});

            //foreach(Project prj in projects)
            //{
            //    chartData.Add(new object[] { prj.ProjectPriority.Name, prj.Tickets.Count() });
            //}

            chartData.Add(new object[] { "Urgent", projects.Where(p => p.ProjectPriority.Name == "Urgent").Count() });
            chartData.Add(new object[] { "High", projects.Where(p => p.ProjectPriority.Name == "High").Count() });
            chartData.Add(new object[] { "Medium", projects.Where(p => p.ProjectPriority.Name == "Medium").Count() });
            chartData.Add(new object[] { "Low", projects.Where(p => p.ProjectPriority.Name == "Low").Count() });

            return Json(chartData);
        }

        public IActionResult Landing()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
