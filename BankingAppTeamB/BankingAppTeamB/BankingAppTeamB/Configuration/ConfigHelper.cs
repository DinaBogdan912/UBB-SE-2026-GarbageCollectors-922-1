using System.IO;
using Microsoft.Extensions.Configuration;

namespace BankingAppTeamB.Configuration;

public static class ConfigHelper
{
    public static string GetConnectionString()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        return config.GetConnectionString("BankingApp");
    }
}