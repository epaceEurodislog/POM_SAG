using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using POMsag.Models;
using POMsag.Services;
#pragma warning disable CS8618, CS8625, CS8600, CS8602, CS8603, CS8604, CS8601

namespace POMsag.Services
{
    public class DynamicsApiService : IDynamicsApiService
    {
        private readonly ApiManager _apiManager;
        private readonly Dictionary<string, HttpClient> _httpClients = new Dictionary<string, HttpClient>();
        private readonly Dictionary<string, DateTime> _tokenExpiryTimes = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, string> _accessTokens = new Dictionary<string, string>();

        public DynamicsApiService(ApiManager apiManager)
        {
            _apiManager = apiManager;
        }

        public async Task<string> GetTokenAsync()
        {
            try
            {
                // Obtenir l'API Dynamics 365
                var dynamicsApi = _apiManager.GetApi("Dynamics365");
                if (dynamicsApi == null)
                    throw new Exception("Configuration Dynamics 365 non trouvée");

                // Vérifier si le token existe et n'est pas expiré
                if (_accessTokens.ContainsKey(dynamicsApi.Name) &&
                    DateTime.Now < _tokenExpiryTimes.GetValueOrDefault(dynamicsApi.Name))
                {
                    return _accessTokens[dynamicsApi.Name];
                }

                // Récupérer les propriétés d'authentification
                if (!dynamicsApi.AuthProperties.TryGetValue("TokenUrl", out string tokenUrl) ||
                    !dynamicsApi.AuthProperties.TryGetValue("ClientId", out string clientId) ||
                    !dynamicsApi.AuthProperties.TryGetValue("ClientSecret", out string clientSecret) ||
                    !dynamicsApi.AuthProperties.TryGetValue("Resource", out string resource))
                {
                    throw new Exception("Paramètres OAuth2 incomplets");
                }

                var requestContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("resource", resource)
                });

                using var client = new HttpClient();
                var response = await client.PostAsync(tokenUrl, requestContent);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                LoggerService.Log("Réponse du serveur OAuth reçue");

                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var accessToken = tokenResponse.GetProperty("access_token").GetString();

                // Gérer la durée de validité du token
                int expiresIn;
                var expiresInProp = tokenResponse.GetProperty("expires_in");

                if (expiresInProp.ValueKind == JsonValueKind.String)
                {
                    if (int.TryParse(expiresInProp.GetString(), out int result))
                        expiresIn = result;
                    else
                        expiresIn = 3599; // Valeur par défaut d'une heure moins une seconde
                }
                else
                {
                    expiresIn = expiresInProp.GetInt32();
                }

                // Sauvegarder le token et sa date d'expiration
                _accessTokens[dynamicsApi.Name] = accessToken;
                _tokenExpiryTimes[dynamicsApi.Name] = DateTime.Now.AddSeconds(expiresIn - 60); // Marge de sécurité

                LoggerService.Log($"Token OAuth obtenu pour {dynamicsApi.Name}, valide jusqu'à {_tokenExpiryTimes[dynamicsApi.Name]}");

                return accessToken;
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, "GetTokenAsync");
                throw;
            }
        }

        public async Task<List<ReleasedProduct>> GetReleasedProductsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string itemNumber = null)
        {
            try
            {
                // Récupérer la configuration de l'API Dynamics 365
                var dynamicsApi = _apiManager.GetApi("Dynamics365");
                if (dynamicsApi == null)
                    throw new Exception("Configuration Dynamics 365 non trouvée");

                var endpoint = dynamicsApi.Endpoints.FirstOrDefault(e => e.Name == "ReleasedProductsV2");
                if (endpoint == null)
                    throw new Exception("Endpoint ReleasedProductsV2 non trouvé");

                // S'assurer qu'un HttpClient existe pour cette API
                if (!_httpClients.ContainsKey(dynamicsApi.Name))
                {
                    _httpClients[dynamicsApi.Name] = new HttpClient
                    {
                        BaseAddress = new Uri(dynamicsApi.BaseUrl)
                    };
                }

                var httpClient = _httpClients[dynamicsApi.Name];

                // Obtenir le token d'accès
                string token = await GetTokenAsync();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Construire l'URL de requête
                var uriBuilder = new UriBuilder(dynamicsApi.BaseUrl + endpoint.Path);
                var query = HttpUtility.ParseQueryString(string.Empty);

                // Ajouter les paramètres globaux
                foreach (var param in dynamicsApi.GlobalParameters)
                {
                    query[param.Key] = param.Value;
                }

                // Ajouter les paramètres spécifiques à l'endpoint
                foreach (var param in endpoint.Parameters)
                {
                    query[param.Key] = param.Value;
                }

                // Filtrage par date
                if (endpoint.SupportsDateFiltering && startDate.HasValue && endDate.HasValue)
                {
                    string startDateStr = startDate.Value.ToString(endpoint.DateFormat);
                    string endDateStr = endDate.Value.ToString(endpoint.DateFormat);

                    // Gérer différents formats de filtrage par date
                    if (endpoint.StartDateParamName.Contains("$filter="))
                    {
                        string filter = endpoint.StartDateParamName.Replace("@startDate", startDateStr);
                        if (!string.IsNullOrEmpty(endpoint.EndDateParamName))
                        {
                            filter += " " + endpoint.EndDateParamName.Replace("@endDate", endDateStr);
                        }
                        query["$filter"] = filter.Replace("$filter=", "");
                    }
                    else
                    {
                        query[endpoint.StartDateParamName] = startDateStr;
                        query[endpoint.EndDateParamName] = endDateStr;
                    }
                }

                uriBuilder.Query = query.ToString();

                // Exécuter la requête
                var response = await httpClient.GetAsync(uriBuilder.Uri);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                LoggerService.Log($"Réponse reçue: {content.Substring(0, Math.Min(100, content.Length))}...");

                var result = ParseJsonResponse(content, endpoint.ResponseRootPath);
                LoggerService.Log($"Données récupérées: {result.Count} éléments");

                // Convertir en liste de ReleasedProduct
                var products = new List<ReleasedProduct>();
                foreach (var dict in result)
                {
                    var product = new ReleasedProduct();
                    foreach (var kvp in dict)
                    {
                        // Mapper les propriétés standard
                        switch (kvp.Key)
                        {
                            case "@odata.etag":
                                product.ODataEtag = kvp.Value?.ToString();
                                break;
                            case "dataAreaId":
                                product.DataAreaId = kvp.Value?.ToString();
                                break;
                            case "ItemNumber":
                                product.ItemNumber = kvp.Value?.ToString();
                                break;
                            case "ProductName":
                                product.ProductName = kvp.Value?.ToString();
                                break;
                            default:
                                // Stocker les propriétés supplémentaires
                                product.AdditionalProperties[kvp.Key] = kvp.Value;
                                break;
                        }
                    }
                    products.Add(product);
                }

                return products;
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, "GetReleasedProductsAsync");
                throw;
            }
        }

        private List<Dictionary<string, object>> ParseJsonResponse(string jsonContent, string rootPath)
        {
            try
            {
                var result = new List<Dictionary<string, object>>();

                if (string.IsNullOrWhiteSpace(jsonContent))
                    return result;

                using (var document = JsonDocument.Parse(jsonContent))
                {
                    JsonElement root = document.RootElement;

                    // Si un chemin racine est spécifié, naviguer jusqu'à cet élément
                    if (!string.IsNullOrWhiteSpace(rootPath))
                    {
                        var pathParts = rootPath.Split('.');

                        foreach (var part in pathParts)
                        {
                            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(part, out var property))
                            {
                                root = property;
                            }
                            else
                            {
                                // Chemin non trouvé, retourner liste vide
                                return result;
                            }
                        }
                    }

                    // Extraire les données selon le type
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in root.EnumerateArray())
                        {
                            var dict = new Dictionary<string, object>();

                            if (item.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var property in item.EnumerateObject())
                                {
                                    dict[property.Name] = ExtractValue(property.Value);
                                }

                                result.Add(dict);
                            }
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                        // Si c'est un objet unique plutôt qu'un tableau
                        var dict = new Dictionary<string, object>();

                        foreach (var property in root.EnumerateObject())
                        {
                            dict[property.Name] = ExtractValue(property.Value);
                        }

                        result.Add(dict);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, "ParseJsonResponse");
                throw;
            }
        }

        private object ExtractValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    if (element.TryGetInt64(out long longValue))
                        return longValue;
                    if (element.TryGetDecimal(out decimal decimalValue))
                        return decimalValue;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        obj[property.Name] = ExtractValue(property.Value);
                    }
                    return obj;
                case JsonValueKind.Array:
                    var array = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        array.Add(ExtractValue(item));
                    }
                    return array;
                default:
                    return null;
            }
        }

        public async Task<List<Dictionary<string, object>>> FetchDataAsync(
            string apiName,
            string endpointName,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                if (endpointName == "ReleasedProductsV2")
                {
                    var products = await GetReleasedProductsAsync(startDate, endDate);

                    // Convertir les ReleasedProduct en Dictionary<string, object>
                    var result = new List<Dictionary<string, object>>();
                    foreach (var product in products)
                    {
                        var dict = product.ToDictionary();
                        result.Add(dict);
                    }

                    return result;
                }
                else
                {
                    throw new NotImplementedException($"L'endpoint '{endpointName}' n'est pas implémenté");
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"FetchDataAsync - {endpointName}");
                throw;
            }
        }
    }
}