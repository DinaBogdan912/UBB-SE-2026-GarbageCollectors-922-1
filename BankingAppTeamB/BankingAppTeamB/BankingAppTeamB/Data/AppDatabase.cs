using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace BankingAppTeamB.Data
{
    public static class AppDatabase
    {
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
