using System;
using System.Collections.Generic;
using BankingAppTeamB.Models;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAppTeamB.Services
{
    public interface IUserSessionService
    {
        int CurrentUserId { get; }
        string CurrentUserName { get; }
        List<Account> GetAccounts();
    }
}
