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

        // Base de données
        public string DatabaseConnectionString { get; private set; }

        // Paramètres généraux pour l'application
        public int MaxRecords { get; private set; }

        // Liste des API configurées
        public List<ApiConfiguration> ConfiguredApis { get; private set; } = new List<ApiConfiguration>();

        // Préférences de sélection des champs
        public Dictionary<string, FieldSelectionPreference> FieldSelections { get; private set; } = new Dictionary<string, FieldSelectionPreference>();

        // Propriétés dérivées pour la compatibilité avec le code existant
        public string ApiUrl => GetLegacyApiUrl();
        public string ApiKey => GetLegacyApiKey();
        public string TokenUrl => GetDynamicsTokenUrl();
        public string ClientId => GetDynamicsClientId();
        public string ClientSecret => GetDynamicsClientSecret();
        public string Resource => GetDynamicsResource();
        public string DynamicsApiUrl => GetDynamicsApiUrl();
        public string SpecificItemNumber { get; private set; }

        public AppConfiguration()
        {
            LoadConfiguration();
        }

        #region Propriétés dérivées pour la compatibilité

        private string GetLegacyApiUrl()
        {
            var api = ConfiguredApis.FirstOrDefault(a => a.ApiId == "pom");
            return api?.BaseUrl ?? "http://localhost:5001/";
        }

        private string GetLegacyApiKey()
        {
            var api = ConfiguredApis.FirstOrDefault(a => a.ApiId == "pom");
            if (api != null && api.AuthType == AuthenticationType.ApiKey)
            {
                api.AuthParameters.TryGetValue("Value", out string apiKey);
                return apiKey ?? "";
            }
            return "";
        }

        private string GetDynamicsTokenUrl()
        {
            var api = ConfiguredApis.FirstOrDefault(a => a.ApiId == "dynamics");
            if (api != null && api.AuthType == AuthenticationType.OAuth2ClientCredentials)
            {
                api.AuthParameters.TryGetValue("TokenUrl", out string tokenUrl);
                return tokenUrl ?? "https://login.microsoftonline.com/6d38d227-4a4d-4fd8-8000-7e5e4f015d7d/oauth2/token";
            }
            return "https://login.microsoftonline.com/6d38d227-4a4d-4fd8-8000-7e5e4f015d7d/oauth2/token";
        }

        private string GetDynamicsClientId()
        {
            var api = ConfiguredApis.FirstOrDefault(a => a.ApiId == "dynamics");
            if (api != null && api.AuthType == AuthenticationType.OAuth2ClientCredentials)
            {
                api.AuthParameters.TryGetValue("ClientId", out string clientId);
                return clientId ?? "";
            }
            return "";
        }

        private string GetDynamicsClientSecret()
        {
            var api = ConfiguredApis.FirstOrDefault(a => a.ApiId == "dynamics");
            if (api != null && api.AuthType == AuthenticationType.OAuth2ClientCredentials)
            {
                api.AuthParameters.TryGetValue("ClientSecret", out string clientSecret);
                return clientSecret ?? "";
            }
            return "";
        }

        private string GetDynamicsResource()
        {
            var api = ConfiguredApis.FirstOrDefault(a => a.ApiId == "dynamics");
            if (api != null && api.AuthType == AuthenticationType.OAuth2ClientCredentials)
            {
                api.AuthParameters.TryGetValue("Resource", out string resource);
                return resource ?? "https://br-uat.sandbox.operations.eu.dynamics.com/";
            }
            return "https://br-uat.sandbox.operations.eu.dynamics.com/";
        }

        private string GetDynamicsApiUrl()
        {
            var api = ConfiguredApis.FirstOrDefault(a => a.ApiId == "dynamics");
            return api?.BaseUrl ?? "https://br-uat.sandbox.operations.eu.dynamics.com/data";
        }

        #endregion

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

                // Paramètres de base de données
                DatabaseConnectionString = configData["Settings"]["DatabaseConnectionString"];

                // Paramètres généraux
                if (int.TryParse(configData["Settings"]["MaxRecords"], out int maxRecords))
                    MaxRecords = maxRecords;
                else
                    MaxRecords = 500;

                SpecificItemNumber = configData["Settings"]["SpecificItemNumber"] ?? "";

                // Charger les préférences de sélection des champs
                LoadFieldSelections(configData);

                // Charger les API configurées
                LoadApiConfigurations(configData);
            }
            catch (Exception ex)
            {
                // Valeurs par défaut
                DatabaseConnectionString = "Server=192.168.9.13\\SQLEXPRESS;Database=pom;User Id=eurodislog;Password=euro;TrustServerCertificate=True;Encrypt=False";
                MaxRecords = 500;
                SpecificItemNumber = "";

                // Créer les API par défaut
                CreateDefaultApis();

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

            // Section Settings
            configData["Settings"]["DatabaseConnectionString"] = "Server=192.168.9.13\\SQLEXPRESS;Database=pom;User Id=eurodislog;Password=euro;TrustServerCertificate=True;Encrypt=False";
            configData["Settings"]["MaxRecords"] = "500";
            configData["Settings"]["SpecificItemNumber"] = "";

            // Section FieldSelections
            configData["FieldSelections"]["Preferences"] = "{}";

            // Section ApiConfigurations
            configData["ApiConfigurations"]["Apis"] = "[]";

            parser.WriteFile(CONFIG_FILE, configData);
        }

        private void CreateDefaultApis()
        {
            ConfiguredApis = new List<ApiConfiguration>();

            // Création de l'API POM par défaut
            var pomApi = new ApiConfiguration("pom", "API POM", "http://localhost:5001/")
            {
                AuthType = AuthenticationType.ApiKey,
                AuthParameters = new Dictionary<string, string> {
                    { "HeaderName", "X-Api-Key" },
                    { "Value", "" }
                }
            };

            // Ajout des endpoints POM
            pomApi.Endpoints.Add(new ApiEndpoint("clients", "api/clients")
            {
                SupportsDateFiltering = true,
                DateStartParamName = "startDate",
                DateEndParamName = "endDate",
                DateFormat = "yyyyMMdd"
            });

            pomApi.Endpoints.Add(new ApiEndpoint("commandes", "api/commandes")
            {
                SupportsDateFiltering = true,
                DateStartParamName = "startDate",
                DateEndParamName = "endDate",
                DateFormat = "yyyyMMdd"
            });

            pomApi.Endpoints.Add(new ApiEndpoint("produits", "api/produits")
            {
                SupportsDateFiltering = false
            });

            pomApi.Endpoints.Add(new ApiEndpoint("lignescommandes", "api/lignescommandes")
            {
                SupportsDateFiltering = true,
                DateStartParamName = "startDate",
                DateEndParamName = "endDate",
                DateFormat = "yyyyMMdd"
            });

            ConfiguredApis.Add(pomApi);

            // Création de l'API Dynamics 365 par défaut
            var d365Api = new ApiConfiguration("dynamics", "Dynamics 365", "https://br-uat.sandbox.operations.eu.dynamics.com/data")
            {
                AuthType = AuthenticationType.OAuth2ClientCredentials,
                AuthParameters = new Dictionary<string, string> {
                    { "TokenUrl", "https://login.microsoftonline.com/6d38d227-4a4d-4fd8-8000-7e5e4f015d7d/oauth2/token" },
                    { "ClientId", "" },
                    { "ClientSecret", "" },
                    { "Resource", "https://br-uat.sandbox.operations.eu.dynamics.com/" }
                }
            };

            // Ajout des endpoints Dynamics
            d365Api.Endpoints.Add(new ApiEndpoint("ReleasedProductsV2", "ReleasedProductsV2")
            {
                SupportsDateFiltering = true,
                DateStartParamName = "", // Format spécial pour Dynamics
                DateEndParamName = "",   // Format spécial pour Dynamics
                DateFormat = "yyyy-MM-ddTHH:mm:ssZ"
            });

            ConfiguredApis.Add(d365Api);

            // Sauvegarder les API par défaut
            SaveApiConfigurations();
        }

        public void SaveConfiguration(
            string connectionString,
            int maxRecords,
            string specificItemNumber)
        {
            var parser = new FileIniDataParser();
            IniData configData = parser.ReadFile(CONFIG_FILE);

            // Section Settings
            configData["Settings"]["DatabaseConnectionString"] = connectionString;
            configData["Settings"]["MaxRecords"] = maxRecords.ToString();
            configData["Settings"]["SpecificItemNumber"] = specificItemNumber;

            // Section FieldSelections (préserver les préférences existantes)
            string fieldSelectionsJson = JsonSerializer.Serialize(FieldSelections);
            configData["FieldSelections"]["Preferences"] = fieldSelectionsJson;

            // Section ApiConfigurations (préserver les API configurées)
            string apisJson = JsonSerializer.Serialize(ConfiguredApis);
            configData["ApiConfigurations"]["Apis"] = apisJson;

            parser.WriteFile(CONFIG_FILE, configData);

            // Mettre à jour les propriétés de l'instance
            DatabaseConnectionString = connectionString;
            MaxRecords = maxRecords;
            SpecificItemNumber = specificItemNumber;
        }

        public void UpdateDynamicsConfig(
            string tokenUrl,
            string clientId,
            string clientSecret,
            string resource,
            string dynamicsApiUrl)
        {
            var dynamicsApi = ConfiguredApis.FirstOrDefault(a => a.ApiId == "dynamics");

            if (dynamicsApi != null)
            {
                // Mettre à jour l'URL de base
                dynamicsApi.BaseUrl = dynamicsApiUrl;

                // Mettre à jour les paramètres d'authentification
                if (dynamicsApi.AuthParameters == null)
                    dynamicsApi.AuthParameters = new Dictionary<string, string>();

                dynamicsApi.AuthParameters["TokenUrl"] = tokenUrl;
                dynamicsApi.AuthParameters["ClientId"] = clientId;
                dynamicsApi.AuthParameters["ClientSecret"] = clientSecret;
                dynamicsApi.AuthParameters["Resource"] = resource;
            }
            else
            {
                // Créer une nouvelle API Dynamics si elle n'existe pas
                var newDynamicsApi = new ApiConfiguration("dynamics", "Dynamics 365", dynamicsApiUrl)
                {
                    AuthType = AuthenticationType.OAuth2ClientCredentials,
                    AuthParameters = new Dictionary<string, string> {
                        { "TokenUrl", tokenUrl },
                        { "ClientId", clientId },
                        { "ClientSecret", clientSecret },
                        { "Resource", resource }
                    }
                };

                // Ajouter l'endpoint par défaut
                newDynamicsApi.Endpoints.Add(new ApiEndpoint("ReleasedProductsV2", "ReleasedProductsV2")
                {
                    SupportsDateFiltering = true,
                    DateStartParamName = "",
                    DateEndParamName = "",
                    DateFormat = "yyyy-MM-ddTHH:mm:ssZ"
                });

                ConfiguredApis.Add(newDynamicsApi);
            }

            // Sauvegarder la configuration
            SaveApiConfigurations();
        }

        public void UpdatePomConfig(
            string apiUrl,
            string apiKey)
        {
            var pomApi = ConfiguredApis.FirstOrDefault(a => a.ApiId == "pom");

            if (pomApi != null)
            {
                // Mettre à jour l'URL de base
                pomApi.BaseUrl = apiUrl;

                // Mettre à jour la clé API
                if (pomApi.AuthParameters == null)
                    pomApi.AuthParameters = new Dictionary<string, string>();

                pomApi.AuthParameters["Value"] = apiKey;
            }
            else
            {
                // Créer une nouvelle API POM si elle n'existe pas
                var newPomApi = new ApiConfiguration("pom", "API POM", apiUrl)
                {
                    AuthType = AuthenticationType.ApiKey,
                    AuthParameters = new Dictionary<string, string> {
                        { "HeaderName", "X-Api-Key" },
                        { "Value", apiKey }
                    }
                };

                // Ajouter les endpoints par défaut
                newPomApi.Endpoints.Add(new ApiEndpoint("clients", "api/clients")
                {
                    SupportsDateFiltering = true,
                    DateStartParamName = "startDate",
                    DateEndParamName = "endDate",
                    DateFormat = "yyyyMMdd"
                });

                ConfiguredApis.Add(newPomApi);
            }

            // Sauvegarder la configuration
            SaveApiConfigurations();
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