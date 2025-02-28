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

        // Nouveaux paramètres pour D365
        public string TokenUrl { get; private set; }
        public string ClientId { get; private set; }
        public string ClientSecret { get; private set; }
        public string Resource { get; private set; }
        public string DynamicsApiUrl { get; private set; }
        public int MaxRecords { get; private set; }

        // Nouveau paramètre pour numéro d'article spécifique
        public string SpecificItemNumber { get; private set; }

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

                // Charger les paramètres D365
                TokenUrl = configData["D365"]["TokenUrl"];
                ClientId = configData["D365"]["ClientId"];
                ClientSecret = configData["D365"]["ClientSecret"];
                Resource = configData["D365"]["Resource"];
                DynamicsApiUrl = configData["D365"]["DynamicsApiUrl"];

                // Charger le nombre maximal d'enregistrements
                if (int.TryParse(configData["D365"]["MaxRecords"], out int maxRecords))
                    MaxRecords = maxRecords;
                else
                    MaxRecords = 500; // Valeur par défaut

                // Charger le numéro d'article spécifique
                SpecificItemNumber = configData["D365"]["SpecificItemNumber"] ?? "";
            }
            catch (Exception ex)
            {
                // Valeurs par défaut
                ApiUrl = "http://localhost:5001/";
                ApiKey = "";
                DatabaseConnectionString = "";

                // Valeurs par défaut pour D365
                TokenUrl = "https://login.microsoftonline.com/6d38d227-4a4d-4fd8-8000-7e5e4f015d7d/oauth2/token";
                ClientId = "";
                ClientSecret = "";
                Resource = "https://br-uat.sandbox.operations.eu.dynamics.com/";
                DynamicsApiUrl = "https://br-uat.sandbox.operations.eu.dynamics.com/data";
                MaxRecords = 500;
                SpecificItemNumber = ""; // Valeur par défaut pour le numéro d'article

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

            // Section Settings existante
            configData["Settings"]["ApiUrl"] = "http://localhost:5001/";
            configData["Settings"]["ApiKey"] = "";
            configData["Settings"]["DatabaseConnectionString"] = "Server=192.168.9.13\\SQLEXPRESS;Database=pom;User Id=eurodislog;Password=euro;TrustServerCertificate=True;Encrypt=False";

            // Nouvelle section D365
            configData["D365"]["TokenUrl"] = "https://login.microsoftonline.com/6d38d227-4a4d-4fd8-8000-7e5e4f015d7d/oauth2/token";
            configData["D365"]["ClientId"] = "";
            configData["D365"]["ClientSecret"] = "";
            configData["D365"]["Resource"] = "https://br-uat.sandbox.operations.eu.dynamics.com/";
            configData["D365"]["DynamicsApiUrl"] = "https://br-uat.sandbox.operations.eu.dynamics.com/data";
            configData["D365"]["MaxRecords"] = "500";
            configData["D365"]["SpecificItemNumber"] = ""; // Nouveau champ

            parser.WriteFile(CONFIG_FILE, configData);
        }

        public void SaveConfiguration(
            string apiUrl,
            string apiKey,
            string connectionString,
            string tokenUrl,
            string clientId,
            string clientSecret,
            string resource,
            string dynamicsApiUrl,
            int maxRecords,
            string specificItemNumber) // Nouveau paramètre
        {
            var parser = new FileIniDataParser();
            IniData configData = new IniData();

            // Section Settings existante
            configData["Settings"]["ApiUrl"] = apiUrl;
            configData["Settings"]["ApiKey"] = apiKey;
            configData["Settings"]["DatabaseConnectionString"] = connectionString;

            // Section D365
            configData["D365"]["TokenUrl"] = tokenUrl;
            configData["D365"]["ClientId"] = clientId;
            configData["D365"]["ClientSecret"] = clientSecret;
            configData["D365"]["Resource"] = resource;
            configData["D365"]["DynamicsApiUrl"] = dynamicsApiUrl;
            configData["D365"]["MaxRecords"] = maxRecords.ToString();
            configData["D365"]["SpecificItemNumber"] = specificItemNumber; // Nouveau champ

            parser.WriteFile(CONFIG_FILE, configData);

            // Mettre à jour les propriétés de l'instance
            ApiUrl = apiUrl;
            ApiKey = apiKey;
            DatabaseConnectionString = connectionString;
            TokenUrl = tokenUrl;
            ClientId = clientId;
            ClientSecret = clientSecret;
            Resource = resource;
            DynamicsApiUrl = dynamicsApiUrl;
            MaxRecords = maxRecords;
            SpecificItemNumber = specificItemNumber;
        }
    }
}