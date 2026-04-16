using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace BankingAppTeamB.Data
{
    public static class DatabaseInitializer
    {
        public static void Initialize()
        {
            string databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
            if (!Directory.Exists(databasePath)) return;
            var scripts = Directory.GetFiles(databasePath, "*.sql");
            Array.Sort(scripts);

            using var connection = AppDatabase.GetConnection();
            connection.Open();

            foreach (var script in scripts)
            {
                try
                {
                    string sql = File.ReadAllText(script);
                    using var command = new SqlCommand(sql, connection);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to execute script '{script}': {ex.Message}", ex);
                }
            }
        }
    }
}
