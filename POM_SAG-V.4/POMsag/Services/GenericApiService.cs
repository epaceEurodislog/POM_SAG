// Fichier: POMsag/Services/GenericApiService.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using POMsag.Models;

namespace POMsag.Services
{
    public class GenericApiService
    {
        private readonly AppConfiguration _configuration;
        private Dictionary<string, HttpClient> _httpClients = new Dictionary<string, HttpClient>();
        private Dictionary<string, string> _accessTokens = new Dictionary<string, string>();
        private Dictionary<string, DateTime> _tokenExpiryTimes = new Dictionary<string, DateTime>();

        public GenericApiService(AppConfiguration configuration)
        {
            _configuration = configuration;
            InitializeHttpClients();
        }

        private void InitializeHttpClients()
        {
            _httpClients.Clear();

            foreach (var api in _configuration.ConfiguredApis)
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri(api.BaseUrl)
                };

                // Configurer les en-têtes par défaut
                foreach (var header in api.Headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                // Configurer l'authentification par clé d'API si nécessaire
                if (api.AuthType == AuthenticationType.ApiKey)
                {
                    if (api.AuthParameters.TryGetValue("HeaderName", out string headerName) &&
                        api.AuthParameters.TryGetValue("Value", out string value))
                    {
                        client.DefaultRequestHeaders.Add(headerName, value);
                    }
                }

                _httpClients[api.ApiId] = client;
            }
        }

        public async Task<List<Dictionary<string, object>>> FetchDataAsync(string apiId, string endpointName,
                                                                           DateTime? startDate = null,
                                                                           DateTime? endDate = null)
        {
            var api = _configuration.GetApiById(apiId);
            if (api == null)
                throw new ArgumentException($"API non trouvée: {apiId}");

            var endpoint = api.Endpoints.Find(e => e.Name == endpointName);
            if (endpoint == null)
                throw new ArgumentException($"Endpoint non trouvé: {endpointName}");

            // Obtenir le client HTTP pour cette API
            if (!_httpClients.TryGetValue(apiId, out HttpClient client))
            {
                // Créer un nouveau client si nécessaire
                client = new HttpClient { BaseAddress = new Uri(api.BaseUrl) };
                _httpClients[apiId] = client;
            }

            // Gérer l'authentification pour cette API
            await HandleAuthenticationAsync(api, client);

            // Construire l'URL avec les paramètres de date si nécessaire
            string url = endpoint.Path;

            if (endpoint.SupportsDateFiltering && startDate.HasValue && endDate.HasValue)
            {
                // Formater l'URL selon l'API
                if (api.ApiId == "dynamics")
                {
                    // Format spécifique pour Dynamics 365 OData
                    string formattedStartDate = startDate.Value.ToString(endpoint.DateFormat);
                    string formattedEndDate = endDate.Value.ToString(endpoint.DateFormat);

                    // Pour OData, les filtres sont ajoutés avec $filter=
                    url += $"?cross-company=true&$filter={endpoint.DateStartParamName} {formattedStartDate} and {endpoint.DateEndParamName} {formattedEndDate}";

                    // Ajouter la limite si configurée
                    if (api.ApiId == "dynamics" && int.TryParse(_configuration.MaxRecords.ToString(), out int maxRecords) && maxRecords > 0)
                    {
                        url += $"&$top={maxRecords}";
                    }
                }
                else
                {
                    // Format par défaut pour les API REST standard
                    string formattedStartDate = startDate.Value.ToString(endpoint.DateFormat);
                    string formattedEndDate = endDate.Value.ToString(endpoint.DateFormat);

                    // Ajouter les paramètres à l'URL
                    string startParamName = !string.IsNullOrEmpty(endpoint.DateStartParamName) ? endpoint.DateStartParamName : "startDate";
                    string endParamName = !string.IsNullOrEmpty(endpoint.DateEndParamName) ? endpoint.DateEndParamName : "endDate";

                    // Vérifier si l'URL contient déjà des paramètres
                    if (url.Contains("?"))
                        url += $"&{startParamName}={formattedStartDate}&{endParamName}={formattedEndDate}";
                    else
                        url += $"?{startParamName}={formattedStartDate}&{endParamName}={formattedEndDate}";
                }
            }

            LoggerService.Log($"Requête vers {api.Name} ({endpoint.Name}): {url}");

            // Exécuter la requête
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            LoggerService.Log($"Réponse reçue ({content.Length} caractères)");

            // Traiter la réponse selon le format attendu
            if (api.ApiId == "dynamics")
            {
                // Dynamics 365 retourne les données dans un objet value
                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    if (doc.RootElement.TryGetProperty("value", out JsonElement valueElement))
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };

                        var items = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                            valueElement.GetRawText(),
                            options
                        );

                        return items ?? new List<Dictionary<string, object>>();
                    }
                }
            }

            // Format standard pour les autres API
            var result = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            return result ?? new List<Dictionary<string, object>>();
        }

        private async Task HandleAuthenticationAsync(ApiConfiguration api, HttpClient client)
        {
            // Authentification OAuth2 pour Dynamics 365
            if (api.AuthType == AuthenticationType.OAuth2ClientCredentials)
            {
                // Vérifier si nous avons déjà un token valide
                if (_accessTokens.TryGetValue(api.ApiId, out string token) &&
                    _tokenExpiryTimes.TryGetValue(api.ApiId, out DateTime expiry) &&
                    DateTime.Now < expiry)
                {
                    // Utiliser le token existant
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    return;
                }

                // Obtenir un nouveau token
                if (api.AuthParameters.TryGetValue("TokenUrl", out string tokenUrl) &&
                    api.AuthParameters.TryGetValue("ClientId", out string clientId) &&
                    api.AuthParameters.TryGetValue("ClientSecret", out string clientSecret) &&
                    api.AuthParameters.TryGetValue("Resource", out string resource))
                {
                    using var tokenClient = new HttpClient();

                    var values = new Dictionary<string, string>
                    {
                        { "client_id", clientId },
                        { "client_secret", clientSecret },
                        { "grant_type", "client_credentials" },
                        { "resource", resource }
                    };

                    var tokenResponse = await tokenClient.PostAsync(tokenUrl, new FormUrlEncodedContent(values));
                    tokenResponse.EnsureSuccessStatusCode();

                    var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                    using var tokenDoc = JsonDocument.Parse(tokenJson);

                    token = tokenDoc.RootElement.GetProperty("access_token").GetString();

                    var expiresIn = 3600; // Valeur par défaut (1 heure)
                    if (tokenDoc.RootElement.TryGetProperty("expires_in", out JsonElement expiresInEl))
                    {
                        if (expiresInEl.ValueKind == JsonValueKind.Number)
                        {
                            expiresIn = expiresInEl.GetInt32();
                        }
                        else if (expiresInEl.ValueKind == JsonValueKind.String)
                        {
                            int.TryParse(expiresInEl.GetString(), out expiresIn);
                        }
                    }

                    _accessTokens[api.ApiId] = token;
                    _tokenExpiryTimes[api.ApiId] = DateTime.Now.AddSeconds(expiresIn - 300); // 5 minutes de marge

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
            else if (api.AuthType == AuthenticationType.Basic)
            {
                // Authentification HTTP Basic
                if (api.AuthParameters.TryGetValue("Username", out string username) &&
                    api.AuthParameters.TryGetValue("Password", out string password))
                {
                    var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
                }
            }
        }
    }
}