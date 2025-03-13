using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using POMsag.Models;

namespace POMsag.Services
{
    public class DynamicApiService
    {
        private readonly ApiManager _apiManager;
        private readonly Dictionary<string, HttpClient> _httpClients = new Dictionary<string, HttpClient>();
        private readonly Dictionary<string, DateTime> _tokenExpiryTimes = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, string> _accessTokens = new Dictionary<string, string>();

        public DynamicApiService(ApiManager apiManager)
        {
            _apiManager = apiManager;
        }

        public async Task<List<Dictionary<string, object>>> FetchDataAsync(
            string apiName,
            string endpointName,
            DateTime? startDate = null,
            DateTime? endDate = null,
            Dictionary<string, string> additionalParams = null)
        {
            try
            {
                LoggerService.Log($"FetchDataAsync - API: {apiName}, Endpoint: {endpointName}");

                // Récupérer la définition de l'API et du endpoint
                var api = _apiManager.GetApi(apiName);
                if (api == null)
                    throw new Exception($"API '{apiName}' non trouvée");

                var endpoint = api.Endpoints.Find(e => e.Name.Equals(endpointName, StringComparison.OrdinalIgnoreCase));
                if (endpoint == null)
                    throw new Exception($"Endpoint '{endpointName}' non trouvé dans l'API '{apiName}'");

                // S'assurer qu'un HttpClient existe pour cette API
                if (!_httpClients.ContainsKey(apiName))
                {
                    _httpClients[apiName] = new HttpClient
                    {
                        BaseAddress = new Uri(api.BaseUrl)
                    };
                }

                var httpClient = _httpClients[apiName];

                // Construire l'URL avec les paramètres
                string url = await BuildRequestUrlAsync(api, endpoint, startDate, endDate, additionalParams);
                LoggerService.Log($"URL construite: {url}");

                // Configurer l'authentification
                await ConfigureAuthenticationAsync(httpClient, api);

                // Ajouter les en-têtes personnalisés
                foreach (var header in api.Headers)
                {
                    httpClient.DefaultRequestHeaders.Remove(header.Key);
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                // Exécuter la requête selon la méthode
                HttpResponseMessage response;
                switch (endpoint.Method)
                {
                    case HttpMethod.Get:
                        response = await httpClient.GetAsync(url);
                        break;
                    case HttpMethod.Post:
                        // Pour les requêtes POST, on pourrait ajouter un corps plus tard
                        response = await httpClient.PostAsync(url, new StringContent("{}"));
                        break;
                    default:
                        throw new NotImplementedException($"Méthode HTTP '{endpoint.Method}' non implémentée");
                }

                response.EnsureSuccessStatusCode();

                // Lire le contenu de la réponse
                var content = await response.Content.ReadAsStringAsync();
                LoggerService.Log($"Réponse reçue: {content.Substring(0, Math.Min(100, content.Length))}...");

                // Désérialiser la réponse JSON
                var result = ParseJsonResponse(content, endpoint.ResponseRootPath);
                LoggerService.Log($"Données récupérées: {result.Count} éléments");

                return result;
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"FetchDataAsync - {apiName}/{endpointName}");
                throw;
            }
        }

        private async Task<string> BuildRequestUrlAsync(
            ApiDefinition api,
            ApiEndpoint endpoint,
            DateTime? startDate,
            DateTime? endDate,
            Dictionary<string, string> additionalParams)
        {
            var uriBuilder = new UriBuilder(api.BaseUrl);
            var path = endpoint.Path;

            // S'assurer que le chemin commence par / s'il n'est pas déjà inclus dans l'URL de base
            if (!path.StartsWith("/") && !api.BaseUrl.EndsWith("/"))
                path = "/" + path;

            uriBuilder.Path += path;

            var query = HttpUtility.ParseQueryString(string.Empty);

            // Ajouter les paramètres globaux de l'API
            foreach (var param in api.GlobalParameters)
            {
                query[param.Key] = param.Value;
            }

            // Ajouter les paramètres spécifiques au endpoint
            foreach (var param in endpoint.Parameters)
            {
                query[param.Key] = param.Value;
            }

            // Ajouter les paramètres supplémentaires
            if (additionalParams != null)
            {
                foreach (var param in additionalParams)
                {
                    query[param.Key] = param.Value;
                }
            }

            // Gérer le filtrage par date si supporté
            if (endpoint.SupportsDateFiltering && startDate.HasValue && endDate.HasValue)
            {
                string startDateStr = startDate.Value.ToString(endpoint.DateFormat);
                string endDateStr = endDate.Value.ToString(endpoint.DateFormat);

                // Traitement spécial pour les paramètres de date
                if (endpoint.StartDateParamName.Contains("$filter="))
                {
                    // Format OData
                    string filter = endpoint.StartDateParamName.Replace("@startDate", startDateStr);
                    if (!string.IsNullOrEmpty(endpoint.EndDateParamName))
                    {
                        filter += " " + endpoint.EndDateParamName.Replace("@endDate", endDateStr);
                    }
                    query["$filter"] = filter.Replace("$filter=", "");
                }
                else
                {
                    // Format standard
                    query[endpoint.StartDateParamName] = startDateStr;
                    query[endpoint.EndDateParamName] = endDateStr;
                }
            }

            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri.ToString();
        }

        private async Task ConfigureAuthenticationAsync(HttpClient httpClient, ApiDefinition api)
        {
            switch (api.AuthType)
            {
                case ApiAuthType.None:
                    // Aucune authentification nécessaire
                    break;

                case ApiAuthType.ApiKey:
                    // Authentification par clé API (généralement dans l'en-tête)
                    if (api.AuthProperties.TryGetValue("HeaderName", out string headerName) &&
                        api.AuthProperties.TryGetValue("ApiKey", out string apiKey))
                    {
                        if (!httpClient.DefaultRequestHeaders.Contains(headerName))
                        {
                            httpClient.DefaultRequestHeaders.Add(headerName, apiKey);
                        }
                    }
                    break;

                case ApiAuthType.OAuth2:
                    // Authentification OAuth2
                    if (!_accessTokens.ContainsKey(api.Name) || DateTime.Now >= _tokenExpiryTimes.GetValueOrDefault(api.Name))
                    {
                        await GetOAuthTokenAsync(api);
                    }

                    if (_accessTokens.TryGetValue(api.Name, out string token))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                    break;

                case ApiAuthType.Basic:
                    // Authentification HTTP Basic
                    if (api.AuthProperties.TryGetValue("Username", out string username) &&
                        api.AuthProperties.TryGetValue("Password", out string password))
                    {
                        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
                    }
                    break;

                case ApiAuthType.Bearer:
                    // Authentification Bearer token (token fourni directement)
                    if (api.AuthProperties.TryGetValue("Token", out string bearerToken))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                    }
                    break;

                case ApiAuthType.Custom:
                    // Authentification personnalisée (à implémenter selon les besoins)
                    break;
            }
        }

        private async Task GetOAuthTokenAsync(ApiDefinition api)
        {
            try
            {
                if (!api.AuthProperties.TryGetValue("TokenUrl", out string tokenUrl) ||
                    !api.AuthProperties.TryGetValue("ClientId", out string clientId) ||
                    !api.AuthProperties.TryGetValue("ClientSecret", out string clientSecret) ||
                    !api.AuthProperties.TryGetValue("Resource", out string resource))
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
                _accessTokens[api.Name] = accessToken;
                _tokenExpiryTimes[api.Name] = DateTime.Now.AddSeconds(expiresIn - 60); // Marge de sécurité

                LoggerService.Log($"Token OAuth obtenu pour {api.Name}, valide jusqu'à {_tokenExpiryTimes[api.Name]}");
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"GetOAuthTokenAsync pour {api.Name}");
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
    }
}