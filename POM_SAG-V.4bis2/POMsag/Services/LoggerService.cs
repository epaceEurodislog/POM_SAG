using System;
using System.IO;
using System.Threading;
#pragma warning disable CS8618, CS8625, CS8600, CS8602, CS8603, CS8604, CS8601

namespace POMsag.Services
{
    public static class LoggerService
    {
        private static readonly object _lock = new object();
        private const string LOG_FILE = "pom_api_log.txt";

        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    using (var writer = new StreamWriter(LOG_FILE, true))
                    {
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                    }
                }
            }
            catch
            {
                // Absorber les exceptions de journalisation
            }
        }

        public static void LogException(Exception ex, string context = "")
        {
            try
            {
                lock (_lock)
                {
                    using (var writer = new StreamWriter(LOG_FILE, true))
                    {
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ERREUR {(string.IsNullOrEmpty(context) ? "" : $"[{context}]")}");
                        writer.WriteLine($"Message: {ex.Message}");
                        writer.WriteLine($"StackTrace: {ex.StackTrace}");

                        if (ex.InnerException != null)
                        {
                            writer.WriteLine($"InnerException: {ex.InnerException.Message}");
                            writer.WriteLine($"InnerStackTrace: {ex.InnerException.StackTrace}");
                        }

                        writer.WriteLine(new string('-', 80));
                    }
                }
            }
            catch
            {
                // Absorber les exceptions de journalisation
            }
        }
    }
}