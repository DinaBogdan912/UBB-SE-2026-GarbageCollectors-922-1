using System;
using System.IO;
using Microsoft.Data.SqlClient;

namespace BankingAppTeamB.Data
{
    public static class DatabaseInitializer
    {
        /// <summary>Runs all <c>*.sql</c> scripts found in the <c>Database</c> sub-folder of the application base directory, in alphabetical order, to create or update the database schema. No-ops if the folder does not exist.</summary>
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
