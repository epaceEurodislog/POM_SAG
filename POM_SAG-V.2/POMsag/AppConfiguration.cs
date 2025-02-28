using System;
using System.IO;
using System.Windows.Forms;
using IniParser;
using IniParser.Model;

namespace POMsag
{
    public class AppConfiguration
    {
        private const string CONFIG_FILE = "config.ini";

        public string ApiUrl { get; private set; }
        public string ApiKey { get; private set; }
        public string DatabaseConnectionString { get; private set; }

        public AppConfiguration()
        {
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            if (!File.Exists(CONFIG_FILE))
            {
                CreateDefaultConfigFile();
            }

            try
            {
                var parser = new FileIniDataParser();
                IniData configData = parser.ReadFile(CONFIG_FILE);

                ApiUrl = configData["Settings"]["ApiUrl"];
                ApiKey = configData["Settings"]["ApiKey"];
                DatabaseConnectionString = configData["Settings"]["DatabaseConnectionString"];
            }
            catch (Exception ex)
            {
                ApiUrl = "http://localhost:5001/";
                ApiKey = "";
                DatabaseConnectionString = "";

                MessageBox.Show(
                    $"Erreur de lecture de la configuration : {ex.Message}\n" +
                    "Utilisation des valeurs par défaut.",
                    "Avertissement Configuration",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }

        private void CreateDefaultConfigFile()
        {
            var parser = new FileIniDataParser();
            IniData configData = new IniData();

            configData["Settings"]["ApiUrl"] = "http://localhost:5001/";
            configData["Settings"]["ApiKey"] = "";
            configData["Settings"]["DatabaseConnectionString"] = "Server=192.168.9.13\\SQLEXPRESS;Database=pom;User Id=eurodislog;Password=euro;TrustServerCertificate=True;Encrypt=False";

            parser.WriteFile(CONFIG_FILE, configData);
        }

        public void SaveConfiguration(string apiUrl, string apiKey, string connectionString)
        {
            var parser = new FileIniDataParser();
            IniData configData = new IniData();

            configData["Settings"]["ApiUrl"] = apiUrl;
            configData["Settings"]["ApiKey"] = apiKey;
            configData["Settings"]["DatabaseConnectionString"] = connectionString;

            parser.WriteFile(CONFIG_FILE, configData);

            ApiUrl = apiUrl;
            ApiKey = apiKey;
            DatabaseConnectionString = connectionString;
        }
    }
}