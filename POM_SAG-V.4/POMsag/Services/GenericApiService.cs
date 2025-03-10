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

            try
            {
                LoggerService.Log($"Début FetchDataAsync - ApiId: {apiId}, Endpoint: {endpointName}");
                LoggerService.Log($"Configuration API : {JsonSerializer.Serialize(api)}");

                // Configuration du client HTTP avec gestion des certificats
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                using var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri(api.BaseUrl)
                };

                // Authentification
                await HandleAuthenticationAsync(api, client);

                // Construction de l'URL
                string url = endpoint.Path;

                // Ajout des paramètres
                var queryParams = new List<string>();

                // Filtres de date
                if (startDate.HasValue && endDate.HasValue && endpoint.SupportsDateFiltering)
                {
                    var startDateStr = startDate.Value.ToString(endpoint.DateFormat);
                    var endDateStr = endDate.Value.ToString(endpoint.DateFormat);

                    queryParams.Add($"$filter=PurchasePriceDate ge {startDateStr} and PurchasePriceDate le {endDateStr}");
                }

                // Limite des enregistrements
                queryParams.Add($"$top={_configuration.MaxRecords}");

                // Ajout des paramètres cross-company pour Dynamics
                if (apiId == "dynamics")
                {
                    queryParams.Insert(0, "cross-company=true");
                }

                // Ajout des paramètres à l'URL
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                LoggerService.Log($"URL de requête complète : {url}");

                // Configuration des en-têtes
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Exécution de la requête
                var response = await client.GetAsync(url);

                // Vérification de la réponse
                var content = await response.Content.ReadAsStringAsync();

                LoggerService.Log($"Statut de la réponse : {response.StatusCode}");
                LoggerService.Log($"Contenu de la réponse (début) : {content.Substring(0, Math.Min(500, content.Length))}");

                response.EnsureSuccessStatusCode();

                // Options de désérialisation flexibles
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                // Gestion spécifique pour Dynamics
                if (apiId == "dynamics")
                {
                    try
                    {
                        using (var doc = JsonDocument.Parse(content))
                        {
                            if (doc.RootElement.TryGetProperty("value", out JsonElement valueElement))
                            {
                                var items = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                                    valueElement.GetRawText(),
                                    options
                                ) ?? new List<Dictionary<string, object>>();

                                LoggerService.Log($"Nombre d'éléments récupérés : {items.Count}");
                                return items;
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        LoggerService.LogException(ex, "Erreur lors de la désérialisation JSON Dynamics");
                        LoggerService.Log($"Contenu problématique (début) : {content.Substring(0, Math.Min(1000, content.Length))}");
                        throw;
                    }
                }

                // Désérialisation standard
                var result = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, options)
                             ?? new List<Dictionary<string, object>>();

                LoggerService.Log($"Nombre d'éléments récupérés : {result.Count}");
                return result;
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"Erreur complète lors du transfert de données pour {apiId}/{endpointName}");
                throw;
            }
        }
        private async Task HandleAuthenticationAsync(ApiConfiguration api, HttpClient client)
        {
            // Authentification OAuth2 pour Dynamics 365
            if (api.AuthType == AuthenticationType.OAuth2ClientCredentials)
            {
                try
                {
                    // Logging des détails d'authentification
                    LoggerService.Log("Début de l'authentification OAuth2");
                    LoggerService.Log($"Paramètres OAuth : " +
                        $"TokenUrl={api.AuthParameters["TokenUrl"]}, " +
                        $"ClientId={api.AuthParameters["ClientId"]}, " +
                        $"Resource={api.AuthParameters["Resource"]}");

                    // Vérifier si nous avons déjà un token valide
                    if (_accessTokens.TryGetValue(api.ApiId, out string token) &&
                        _tokenExpiryTimes.TryGetValue(api.ApiId, out DateTime expiry) &&
                        DateTime.Now < expiry)
                    {
                        LoggerService.Log("Utilisation du token existant");
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        return;
                    }

                    // Obtenir un nouveau token
                    var tokenUrl = api.AuthParameters["TokenUrl"];
                    var clientId = api.AuthParameters["ClientId"];
                    var clientSecret = api.AuthParameters["ClientSecret"];
                    var resource = api.AuthParameters["Resource"];

                    using var tokenClient = new HttpClient();

                    var values = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "grant_type", "client_credentials" },
                { "resource", resource }
            };

                    LoggerService.Log("Envoi de la requête de token OAuth");
                    var tokenResponse = await tokenClient.PostAsync(tokenUrl, new FormUrlEncodedContent(values));

                    // Vérification de la réponse
                    var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                    LoggerService.Log($"Réponse du token (début) : {tokenContent.Substring(0, Math.Min(500, tokenContent.Length))}");

                    tokenResponse.EnsureSuccessStatusCode();

                    var tokenJson = tokenContent;
                    using var tokenDoc = JsonDocument.Parse(tokenJson);

                    token = tokenDoc.RootElement.GetProperty("access_token").GetString();

                    // Gestion de l'expiration du token
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

                    // Mise à jour du token
                    _accessTokens[api.ApiId] = token;
                    _tokenExpiryTimes[api.ApiId] = DateTime.Now.AddSeconds(expiresIn - 300); // 5 minutes de marge

                    // Ajout du token à l'en-tête
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    LoggerService.Log("Authentification OAuth2 réussie");
                }
                catch (Exception ex)
                {
                    LoggerService.LogException(ex, "Erreur lors de l'authentification OAuth2");
                    throw;
                }
            }
            else if (api.AuthType == AuthenticationType.ApiKey)
            {
                // Authentification par clé API
                if (api.AuthParameters.TryGetValue("HeaderName", out string headerName) &&
                    api.AuthParameters.TryGetValue("Value", out string value))
                {
                    client.DefaultRequestHeaders.Add(headerName, value);
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