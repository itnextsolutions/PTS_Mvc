﻿using MVC_BugTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVC_BugTracker.Services.Interfaces
{
    public interface IBTCompanyInfoService
    {
        Task<Company> GetCompanyInfoByIdAsync(int? companyId);
        public  Task UpdateTicketTask(TicketTask task);
        Task<List<BTUser>> GetAllMembersAsync(int companyId);

        Task<List<BTUser>> GetAllMembersAsync(int companyId,string id);

        Task<List<Project>> GetAllProjectsAsync(int companyId);

        Task<List<Ticket>> GetAllTicketsAsync(int companyId);

        Task<List<TicketTask>> GetAllTicketTask(int? ticketId);

        //Task<List<BTUser>> GetMembersInRoleAsync(string roleName, int companyId,string v);

        Task<List<BTUser>> GetMembersInRoleAsyncAdmin(string roleName, int companyId);

    }
}
