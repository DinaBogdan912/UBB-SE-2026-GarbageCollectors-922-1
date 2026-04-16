using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Services
{
    public interface IUserSessionService
    {
        int CurrentUserId { get; }
        string CurrentUserName { get; }
        List<Account> GetAccounts();
    }
}
