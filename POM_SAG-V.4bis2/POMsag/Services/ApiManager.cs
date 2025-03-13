using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using POMsag.Models;

namespace POMsag.Services
{
    public class ApiManager
    {
        private readonly string _apisDirectory;
        private Dictionary<string, ApiDefinition> _apis = new Dictionary<string, ApiDefinition>();
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiManager(string apisDirectory = "APIs")
        {
            _apisDirectory = apisDirectory;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };

            // Créer le répertoire s'il n'existe pas
            if (!Directory.Exists(_apisDirectory))
            {
                Directory.CreateDirectory(_apisDirectory);
            }

            // Charger toutes les API au démarrage
            LoadAllApis();
        }

        private void LoadAllApis()
        {
            try
            {
                string[] apiFiles = Directory.GetFiles(_apisDirectory, "*.json");
                _apis.Clear();

                foreach (var file in apiFiles)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var api = JsonSerializer.Deserialize<ApiDefinition>(json, _jsonOptions);

                        if (api != null && !string.IsNullOrEmpty(api.Name))
                        {
                            _apis[api.Name] = api;
                            LoggerService.Log($"API chargée: {api.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerService.LogException(ex, $"Erreur lors du chargement du fichier API: {Path.GetFileName(file)}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, "LoadAllApis");
            }
        }

        public bool AddOrUpdateApi(ApiDefinition api)
        {
            try
            {
                if (api == null || string.IsNullOrEmpty(api.Name))
                    return false;

                // Valider l'API
                if (!api.Validate(out List<string> errors))
                {
                    LoggerService.Log($"Erreur de validation de l'API {api.Name}: {string.Join(", ", errors)}");
                    return false;
                }

                // Mettre à jour la date de modification
                api.LastModifiedDate = DateTime.Now;

                // Sauvegarder dans le fichier JSON
                string filePath = Path.Combine(_apisDirectory, $"{api.Name}.json");
                string json = JsonSerializer.Serialize(api, _jsonOptions);
                File.WriteAllText(filePath, json);

                // Mettre à jour le dictionnaire en mémoire
                _apis[api.Name] = api;

                LoggerService.Log($"API sauvegardée: {api.Name}");
                return true;
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"AddOrUpdateApi: {api?.Name}");
                return false;
            }
        }

        public bool DeleteApi(string apiName)
        {
            try
            {
                if (string.IsNullOrEmpty(apiName) || !_apis.ContainsKey(apiName))
                    return false;

                // Supprimer du dictionnaire
                _apis.Remove(apiName);

                // Supprimer le fichier
                string filePath = Path.Combine(_apisDirectory, $"{apiName}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                LoggerService.Log($"API supprimée: {apiName}");
                return true;
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"DeleteApi: {apiName}");
                return false;
            }
        }

        public ApiDefinition GetApi(string apiName)
        {
            if (string.IsNullOrEmpty(apiName) || !_apis.ContainsKey(apiName))
                return null;

            return _apis[apiName];
        }

        public List<ApiDefinition> GetAllApis()
        {
            return _apis.Values.ToList();
        }

        public ApiEndpoint GetEndpoint(string apiName, string endpointName)
        {
            var api = GetApi(apiName);
            if (api == null)
                return null;

            return api.Endpoints.FirstOrDefault(e => e.Name.Equals(endpointName, StringComparison.OrdinalIgnoreCase));
        }

        // Crée des exemples d'API pour démarrer
        public void CreateSampleApis()
        {
            // Exemple d'API POM
            var pomApi = new ApiDefinition
            {
                Name = "POM",
                Description = "API standard POM pour récupérer les données clients, commandes, etc.",
                BaseUrl = "http://localhost:5001/",
                AuthType = ApiAuthType.ApiKey,
                AuthProperties = new Dictionary<string, string>
                {
                    { "HeaderName", "X-Api-Key" },
                    { "ApiKey", "" }
                },
                Endpoints = new List<ApiEndpoint>
                {
                    new ApiEndpoint
                    {
                        Name = "Clients",
                        Description = "Récupère la liste des clients",
                        Path = "api/clients",
                        SupportsDateFiltering = true,
                        StartDateParamName = "startDate",
                        EndDateParamName = "endDate",
                        DateFormat = "yyyyMMdd"
                    },
                    new ApiEndpoint
                    {
                        Name = "Commandes",
                        Description = "Récupère la liste des commandes",
                        Path = "api/commandes",
                        SupportsDateFiltering = true,
                        StartDateParamName = "startDate",
                        EndDateParamName = "endDate",
                        DateFormat = "yyyyMMdd"
                    },
                    new ApiEndpoint
                    {
                        Name = "Produits",
                        Description = "Récupère la liste des produits",
                        Path = "api/produits",
                        SupportsDateFiltering = false
                    },
                    new ApiEndpoint
                    {
                        Name = "LignesCommandes",
                        Description = "Récupère les lignes de commandes",
                        Path = "api/lignescommandes",
                        SupportsDateFiltering = true,
                        StartDateParamName = "startDate",
                        EndDateParamName = "endDate",
                        DateFormat = "yyyyMMdd"
                    }
                }
            };

            // Exemple d'API Dynamics 365
            var dynamicsApi = new ApiDefinition
            {
                Name = "Dynamics365",
                Description = "API Dynamics 365 pour Finance et Opérations",
                BaseUrl = "https://br-uat.sandbox.operations.eu.dynamics.com/data",
                AuthType = ApiAuthType.OAuth2,
                AuthProperties = new Dictionary<string, string>
                {
                    { "TokenUrl", "https://login.microsoftonline.com/6d38d227-4a4d-4fd8-8000-7e5e4f015d7d/oauth2/token" },
                    { "ClientId", "" },
                    { "ClientSecret", "" },
                    { "Resource", "https://br-uat.sandbox.operations.eu.dynamics.com/" }
                },
                Endpoints = new List<ApiEndpoint>
                {
                    new ApiEndpoint
                    {
                        Name = "ReleasedProductsV2",
                        Description = "Récupère les produits publiés",
                        Path = "ReleasedProductsV2",
                        Parameters = new Dictionary<string, string>
                        {
                            { "cross-company", "true" },
                            { "$top", "500" }
                        },
                        SupportsDateFiltering = true,
                        StartDateParamName = "$filter=PurchasePriceDate ge @startDate",
                        EndDateParamName = "and PurchasePriceDate le @endDate",
                        DateFormat = "yyyy-MM-ddT00:00:00Z",
                        ResponseRootPath = "value"
                    }
                }
            };

            // Sauvegarder les exemples
            AddOrUpdateApi(pomApi);
            AddOrUpdateApi(dynamicsApi);
        }
    }
}