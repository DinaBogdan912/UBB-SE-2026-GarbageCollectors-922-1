using System;
using System.IO;
using Microsoft.Data.SqlClient;

namespace BankingAppTeamB.Data
{
    public static class DatabaseInitializer
    {
        public static void Initialize()
        {
            string databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
            if (!Directory.Exists(databasePath))
            {
                return;
            }

            var scripts = Directory.GetFiles(databasePath, "*.sql");
            Array.Sort(scripts);

            using var connection = AppDatabase.GetConnection();
            connection.Open();

            foreach (var script in scripts)
            {
                try
                {
                    string scriptContent = File.ReadAllText(script);
                    using var command = new SqlCommand(scriptContent, connection);
                    command.ExecuteNonQuery();
                }
                catch (Exception executeScriptException)
                {
                    throw new InvalidOperationException($"Failed to execute script '{script}': {executeScriptException.Message}", executeScriptException);
                }
            }
        }
    }
}
