using System;
using System.IO;
using System.Threading;

namespace POMsag.Services
{
    public static class LoggerService
    {
        private static readonly object _lock = new object();
        private const string LOG_FILE = "pom_api_log.txt";
        private static bool _isInitialized = false;

        /// <summary>
        /// Indique si le service de journalisation est initialisé
        /// </summary>
        public static bool IsInitialized
        {
            get { return _isInitialized; }
        }

        static LoggerService()
        {
            try
            {
                // Vérifier si le répertoire de logs existe, sinon le créer
                string logDirectory = Path.GetDirectoryName(LOG_FILE);
                if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // Écrire un message de démarrage pour confirmer que le service est prêt
                using (var writer = new StreamWriter(LOG_FILE, true))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Service de journalisation initialisé");
                }

                _isInitialized = true;
            }
            catch
            {
                // En cas d'erreur, le service n'est pas initialisé
                _isInitialized = false;
            }
        }

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

        /// <summary>
        /// Vide le fichier journal
        /// </summary>
        public static void ClearLogs()
        {
            try
            {
                lock (_lock)
                {
                    File.WriteAllText(LOG_FILE, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Journal effacé\r\n");
                }
            }
            catch
            {
                // Absorber les exceptions de journalisation
            }
        }
    }
}