using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using IniParser;
using IniParser.Model;
using POMsag.Models;
using POMsag.Services;

namespace POMsag
{
    public class AppConfiguration
    {
        private const string CONFIG_FILE = "config.ini";

        public string ApiUrl { get; private set; }
        public string ApiKey { get; private set; }
        public string DatabaseConnectionString { get; private set; }

        // Paramètres pour D365
        public string TokenUrl { get; private set; }
        public string ClientId { get; private set; }
        public string ClientSecret { get; private set; }
        public string Resource { get; private set; }
        public string DynamicsApiUrl { get; private set; }
        public int MaxRecords { get; private set; }

        // Paramètre pour numéro d'article spécifique
        public string SpecificItemNumber { get; private set; }

        // Préférences de sélection des champs
        public Dictionary<string, FieldSelectionPreference> FieldSelections { get; private set; } = new Dictionary<string, FieldSelectionPreference>();

        // Liste des API configurées
        public List<ApiConfiguration> ConfiguredApis { get; private set; } = new List<ApiConfiguration>();

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

                // Charger les préférences de sélection des champs
                LoadFieldSelections(configData);

                // Charger les API configurées
                LoadApiConfigurations(configData);
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

        private void LoadFieldSelections(IniData configData)
        {
            if (configData.Sections.ContainsSection("FieldSelections"))
            {
                string fieldSelectionsJson = configData["FieldSelections"]["Preferences"];
                if (!string.IsNullOrEmpty(fieldSelectionsJson))
                {
                    try
                    {
                        FieldSelections = JsonSerializer.Deserialize<Dictionary<string, FieldSelectionPreference>>(fieldSelectionsJson);
                    }
                    catch (Exception ex)
                    {
                        LoggerService.LogException(ex, "Chargement des préférences de champs");
                        FieldSelections = new Dictionary<string, FieldSelectionPreference>();
                    }
                }
            }
        }

        private void LoadApiConfigurations(IniData configData)
        {
            ConfiguredApis.Clear();

            if (configData.Sections.ContainsSection("ApiConfigurations"))
            {
                string apisJson = configData["ApiConfigurations"]["Apis"];
                if (!string.IsNullOrEmpty(apisJson))
                {
                    try
                    {
                        ConfiguredApis = JsonSerializer.Deserialize<List<ApiConfiguration>>(apisJson);

                        // Si pas d'API configurées, créer les API par défaut
                        if (ConfiguredApis == null || ConfiguredApis.Count == 0)
                        {
                            CreateDefaultApis();
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerService.LogException(ex, "Chargement des configurations d'API");
                        CreateDefaultApis();
                    }
                }
                else
                {
                    CreateDefaultApis();
                }
            }
            else
            {
                CreateDefaultApis();
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

            // Section D365
            configData["D365"]["TokenUrl"] = "https://login.microsoftonline.com/6d38d227-4a4d-4fd8-8000-7e5e4f015d7d/oauth2/token";
            configData["D365"]["ClientId"] = "";
            configData["D365"]["ClientSecret"] = "";
            configData["D365"]["Resource"] = "https://br-uat.sandbox.operations.eu.dynamics.com/";
            configData["D365"]["DynamicsApiUrl"] = "https://br-uat.sandbox.operations.eu.dynamics.com/data";
            configData["D365"]["MaxRecords"] = "500";
            configData["D365"]["SpecificItemNumber"] = ""; // Nouveau champ

            // Section FieldSelections
            configData["FieldSelections"]["Preferences"] = "{}";

            // Section ApiConfigurations
            configData["ApiConfigurations"]["Apis"] = "[]";

            parser.WriteFile(CONFIG_FILE, configData);
        }

        // Dans AppConfiguration.cs, modifiez la méthode CreateDefaultApis()
        private void CreateDefaultApis()
        {
            ConfiguredApis = new List<ApiConfiguration>();

            // Création de l'API Dynamics 365 par défaut (UNIQUEMENT)
            var d365Api = new ApiConfiguration("dynamics", "Dynamics 365", DynamicsApiUrl)
            {
                AuthType = AuthenticationType.OAuth2ClientCredentials,
                AuthParameters = new Dictionary<string, string> {
            { "TokenUrl", TokenUrl },
            { "ClientId", ClientId },
            { "ClientSecret", ClientSecret },
            { "Resource", Resource }
        }
            };

            // Ajout des endpoints Dynamics
            d365Api.Endpoints.Add(new ApiEndpoint("ReleasedProductsV2", "ReleasedProductsV2")
            {
                SupportsDateFiltering = true,
                DateStartParamName = "PurchasePriceDate ge",
                DateEndParamName = "PurchasePriceDate le",
                DateFormat = "yyyy-MM-ddTHH:mm:ssZ"
            });

            ConfiguredApis.Add(d365Api);

            // Sauvegarder les API par défaut
            SaveApiConfigurations();
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
            string specificItemNumber)
        {
            var parser = new FileIniDataParser();
            IniData configData = parser.ReadFile(CONFIG_FILE);

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
            configData["D365"]["SpecificItemNumber"] = specificItemNumber;

            // Section FieldSelections (préserver les préférences existantes)
            string fieldSelectionsJson = JsonSerializer.Serialize(FieldSelections);
            configData["FieldSelections"]["Preferences"] = fieldSelectionsJson;

            // Section ApiConfigurations (préserver les API configurées)
            string apisJson = JsonSerializer.Serialize(ConfiguredApis);
            configData["ApiConfigurations"]["Apis"] = apisJson;

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

        public void AddOrUpdateFieldPreference(string entityName, string fieldName, bool isSelected)
        {
            if (!FieldSelections.ContainsKey(entityName))
                FieldSelections[entityName] = new FieldSelectionPreference(entityName);

            FieldSelections[entityName].AddOrUpdateField(fieldName, isSelected);
            SaveFieldSelections();
        }

        public bool IsFieldSelected(string entityName, string fieldName)
        {
            if (FieldSelections.ContainsKey(entityName) &&
                FieldSelections[entityName].Fields.ContainsKey(fieldName))
                return FieldSelections[entityName].Fields[fieldName];

            return true; // Par défaut, tous les champs sont sélectionnés
        }

        public void SaveFieldSelections()
        {
            try
            {
                var parser = new FileIniDataParser();
                IniData configData = parser.ReadFile(CONFIG_FILE);

                if (!configData.Sections.ContainsSection("FieldSelections"))
                    configData.Sections.AddSection("FieldSelections");

                string json = JsonSerializer.Serialize(FieldSelections);
                configData["FieldSelections"]["Preferences"] = json;

                parser.WriteFile(CONFIG_FILE, configData);
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, "Sauvegarde des préférences de champs");
            }
        }
        public void SaveApiConfigurations()
        {
            try
            {
                var parser = new FileIniDataParser();
                IniData configData = parser.ReadFile(CONFIG_FILE);

                if (!configData.Sections.ContainsSection("ApiConfigurations"))
                    configData.Sections.AddSection("ApiConfigurations");

                string json = JsonSerializer.Serialize(ConfiguredApis);
                configData["ApiConfigurations"]["Apis"] = json;

                parser.WriteFile(CONFIG_FILE, configData);

                LoggerService.Log($"Configurations d'API sauvegardées: {ConfiguredApis.Count} APIs");
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, "Sauvegarde des configurations d'API");
                throw; // Rethrow to handle it at a higher level
            }
        }
        public void AddOrUpdateApi(ApiConfiguration api)
        {
            // Créer une nouvelle instance d'API pour éviter le partage de références
            var apiCopy = new ApiConfiguration(api.ApiId, api.Name, api.BaseUrl)
            {
                AuthType = api.AuthType,
                Headers = new Dictionary<string, string>(api.Headers),
                AuthParameters = new Dictionary<string, string>(api.AuthParameters)
            };

            // Copier les endpoints individuellement pour éviter le partage de références
            apiCopy.Endpoints = new List<ApiEndpoint>();
            foreach (var endpoint in api.Endpoints)
            {
                var endpointCopy = new ApiEndpoint(endpoint.Name, endpoint.Path)
                {
                    Method = endpoint.Method,
                    SupportsDateFiltering = endpoint.SupportsDateFiltering,
                    DateStartParamName = endpoint.DateStartParamName,
                    DateEndParamName = endpoint.DateEndParamName,
                    DateFormat = endpoint.DateFormat
                };
                apiCopy.Endpoints.Add(endpointCopy);
            }

            // Ajouter ou mettre à jour l'API dans la collection
            int index = ConfiguredApis.FindIndex(a => a.ApiId == api.ApiId);
            if (index >= 0)
                ConfiguredApis[index] = apiCopy;
            else
                ConfiguredApis.Add(apiCopy);

            SaveApiConfigurations();
        }

        public void RemoveApi(string apiId)
        {
            ConfiguredApis.RemoveAll(a => a.ApiId == apiId);
            SaveApiConfigurations();
        }

        public ApiConfiguration GetApiById(string apiId)
        {
            return ConfiguredApis.FirstOrDefault(a => a.ApiId == apiId);
        }


    }
}