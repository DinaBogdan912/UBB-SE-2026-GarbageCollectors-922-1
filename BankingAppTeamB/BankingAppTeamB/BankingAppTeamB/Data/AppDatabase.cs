using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace BankingAppTeamB.Data
{
    public static class AppDatabase
    {
        /// <summary>Creates and returns a new SqlConnection configured with the BankingApp connection string from appsettings.json. The caller is responsible for opening and disposing the connection.</summary>
        public static SqlConnection GetConnection()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            string connectionString = configuration.GetConnectionString("BankingApp");
            return new SqlConnection(connectionString);
        }
    }
}
