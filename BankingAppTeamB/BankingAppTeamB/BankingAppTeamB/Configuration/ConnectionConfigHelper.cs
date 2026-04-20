using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace BankingAppTeamB.Configuration;

public static class ConnectionConfigHelper
{
    private const string AppSettingsFileName = "appsettings.json";
    private const string BankingApplicationConnectionStringKey = "BankingApp";

    /// <summary>Reads the <c>BankingApp</c> connection string from <c>appsettings.json</c> and returns it; throws if the key is absent or blank.</summary>
    public static string GetConnectionString()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile(AppSettingsFileName)
            .Build();

        string? bankingApplicationConnectionString = configuration.GetConnectionString(BankingApplicationConnectionStringKey);
        if (string.IsNullOrWhiteSpace(bankingApplicationConnectionString))
        {
            throw new InvalidOperationException($"Connection string '{BankingApplicationConnectionStringKey}' was not found in '{AppSettingsFileName}'.");
        }

        return bankingApplicationConnectionString;
    }
}
