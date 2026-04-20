using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace BankingAppTeamB.Configuration;

public static class ConnectionConfigHelper
{
    public static string GetConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        return configuration.GetConnectionString("BankingApp");
    }
}