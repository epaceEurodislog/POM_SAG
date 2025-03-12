using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using POMsag.Models;
using POMsag.Services;

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
                LoggerService.Log($"Initialisation du client HTTP pour l'API : {api.Name}");
                LoggerService.Log($"BaseURL: {api.BaseUrl}");
                LoggerService.Log($"Type d'authentification: {api.AuthType}");

                var client = new HttpClient
                {
                    BaseAddress = new Uri(api.BaseUrl)
                };

                // Configurer les en-têtes par défaut
                foreach (var header in api.Headers ?? new Dictionary<string, string>())
                {
                    LoggerService.Log($"Ajout de l'en-tête : {header.Key} = {header.Value}");
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                // Configurer l'authentification par clé d'API si nécessaire
                if (api.AuthType == AuthenticationType.ApiKey)
                {
                    if (api.AuthParameters.TryGetValue("HeaderName", out string headerName) &&
                        api.AuthParameters.TryGetValue("Value", out string value))
                    {
                        LoggerService.Log($"Ajout de l'en-tête d'authentification : {headerName}");
                        client.DefaultRequestHeaders.Add(headerName, value);
                    }
                }

                _httpClients[api.ApiId] = client;
            }
        }

        public async Task<List<Dictionary<string, object>>> FetchDataAsync(
            string apiId,
            string endpointName,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            LoggerService.Log("-------- DÉBUT DE FETCHDATAASYNC --------");
            LoggerService.Log($"Paramètres de la requête :");
            LoggerService.Log($"API ID: {apiId}");
            LoggerService.Log($"Endpoint: {endpointName}");
            LoggerService.Log($"Date de début: {startDate}");
            LoggerService.Log($"Date de fin: {endDate}");

            var api = _configuration.GetApiById(apiId);
            if (api == null)
            {
                LoggerService.Log($"ERREUR : API non trouvée pour l'ID {apiId}");
                throw new ArgumentException($"API non trouvée: {apiId}");
            }

            var endpoint = api.Endpoints.Find(e => e.Name == endpointName);
            if (endpoint == null)
            {
                LoggerService.Log($"ERREUR : Endpoint non trouvé pour {endpointName}");
                throw new ArgumentException($"Endpoint non trouvé: {endpointName}");
            }

            LoggerService.Log($"Détails de l'endpoint :");
            LoggerService.Log($"Chemin: {endpoint.Path}");
            LoggerService.Log($"Méthode: {endpoint.Method}");
            LoggerService.Log($"Supporte filtrage par date: {endpoint.SupportsDateFiltering}");

            // Traitement spécial pour Dynamics 365
            if (apiId == "dynamics")
            {
                return await FetchDataFromDynamicsAsync(api, endpoint, startDate, endDate);
            }

            try
            {
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

                    LoggerService.Log($"Filtrage par date activé :");
                    LoggerService.Log($"Date de début formatée: {startDateStr}");
                    LoggerService.Log($"Date de fin formatée: {endDateStr}");

                    // Construction correcte du filtre de date en utilisant les paramètres de l'endpoint
                    if (!string.IsNullOrEmpty(endpoint.DateStartParamName) && !string.IsNullOrEmpty(endpoint.DateEndParamName))
                    {
                        queryParams.Add($"{endpoint.DateStartParamName}={startDateStr}");
                        queryParams.Add($"{endpoint.DateEndParamName}={endDateStr}");
                    }
                }

                // Limite des enregistrements
                int maxRecords = _configuration.MaxRecords > 0 ? _configuration.MaxRecords : 500;
                queryParams.Add($"limit={maxRecords}");
                LoggerService.Log($"Nombre maximal d'enregistrements : {maxRecords}");

                // Ajout des paramètres à l'URL
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                LoggerService.Log($"URL de requête complète : {url}");

                // Préparation des en-têtes
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Journalisation des en-têtes
                LoggerService.Log("En-têtes de la requête :");
                foreach (var header in client.DefaultRequestHeaders)
                {
                    LoggerService.Log($"{header.Key}: {string.Join(", ", header.Value)}");
                }

                // Exécution de la requête
                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync(url);
                }
                catch (Exception ex)
                {
                    LoggerService.Log($"ERREUR lors de l'appel API : {ex.Message}");
                    LoggerService.LogException(ex, "Erreur d'appel API");
                    throw;
                }

                // Lecture du contenu
                var content = await response.Content.ReadAsStringAsync();

                // Journalisation détaillée de la réponse
                LoggerService.Log($"Statut de la réponse : {response.StatusCode}");
                LoggerService.Log($"Type de contenu : {response.Content.Headers.ContentType}");
                LoggerService.Log($"Longueur du contenu : {content.Length} caractères");
                LoggerService.Log($"Début du contenu (500 premiers caractères) :\n{content.Substring(0, Math.Min(500, content.Length))}");

                // Vérification du succès de la requête
                response.EnsureSuccessStatusCode();

                // Options de désérialisation flexibles
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                // Désérialisation standard
                var result = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, options)
                             ?? new List<Dictionary<string, object>>();

                LoggerService.Log($"Nombre d'éléments récupérés : {result.Count}");

                // Ajouter des logs pour examiner les données
                if (result.Count > 0)
                {
                    // Échantillon du premier élément pour débogage
                    var firstItem = result[0];
                    LoggerService.Log("Premier élément récupéré (échantillon) :");
                    foreach (var kvp in firstItem.Take(5)) // Limiter à 5 champs pour éviter des logs trop volumineux
                    {
                        LoggerService.Log($"  - {kvp.Key}: {kvp.Value}");
                    }
                }
                else
                {
                    LoggerService.Log("ATTENTION : Aucun élément récupéré de l'API !");
                }

                LoggerService.Log("-------- FIN DE FETCHDATAASYNC (Standard) --------");
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
            LoggerService.Log("-------- DÉBUT DE L'AUTHENTIFICATION --------");
            LoggerService.Log($"Type d'authentification : {api.AuthType}");

            if (api.AuthType == AuthenticationType.OAuth2ClientCredentials)
            {
                try
                {
                    // Vérifier que tous les paramètres requis sont présents
                    string[] requiredParams = { "TokenUrl", "ClientId", "ClientSecret", "Resource" };
                    foreach (var param in requiredParams)
                    {
                        if (!api.AuthParameters.ContainsKey(param) || string.IsNullOrEmpty(api.AuthParameters[param]))
                        {
                            LoggerService.Log($"ERREUR : Paramètre OAuth {param} manquant ou vide");
                            throw new ArgumentException($"Paramètre OAuth {param} manquant ou vide dans la configuration");
                        }
                    }

                    // Extraction des paramètres
                    var tokenUrl = api.AuthParameters["TokenUrl"];
                    var clientId = api.AuthParameters["ClientId"];
                    var clientSecret = api.AuthParameters["ClientSecret"];
                    var resource = api.AuthParameters["Resource"];

                    LoggerService.Log($"Détails OAuth :");
                    LoggerService.Log($"Token URL : {tokenUrl}");
                    LoggerService.Log($"Client ID : {clientId}");
                    LoggerService.Log($"Resource : {resource}");

                    // Vérifier si nous avons déjà un token valide
                    if (_accessTokens.TryGetValue(api.ApiId, out string token) &&
                        _tokenExpiryTimes.TryGetValue(api.ApiId, out DateTime expiry) &&
                        DateTime.Now < expiry)
                    {
                        LoggerService.Log("Utilisation du token existant");
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        return;
                    }

                    LoggerService.Log("Demande d'un nouveau token OAuth");

                    // Préparation de la requête de token avec un handler spécial pour ignorer les erreurs SSL
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };

                    using var tokenClient = new HttpClient(handler);

                    var values = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "grant_type", "client_credentials" },
                { "resource", resource }
            };

                    // Envoi de la requête
                    var tokenResponse = await tokenClient.PostAsync(tokenUrl, new FormUrlEncodedContent(values));

                    // Lecture du contenu de la réponse
                    var tokenContent = await tokenResponse.Content.ReadAsStringAsync();

                    LoggerService.Log($"Statut de la réponse du token : {tokenResponse.StatusCode}");

                    // Journaliser une version tronquée pour éviter des logs trop volumineux
                    if (tokenContent.Length > 0)
                    {
                        LoggerService.Log($"Début du contenu de la réponse : {tokenContent.Substring(0, Math.Min(500, tokenContent.Length))}");
                    }
                    else
                    {
                        LoggerService.Log("Réponse token vide!");
                    }

                    // Vérification du succès de la requête
                    if (!tokenResponse.IsSuccessStatusCode)
                    {
                        LoggerService.Log($"ERREUR lors de l'obtention du token: {tokenResponse.StatusCode} - {tokenContent}");
                        throw new Exception($"Erreur lors de l'obtention du token: {tokenResponse.StatusCode} - {tokenContent}");
                    }

                    // Analyse du token
                    using var tokenDoc = JsonDocument.Parse(tokenContent);

                    // Extraction du token d'accès
                    if (!tokenDoc.RootElement.TryGetProperty("access_token", out JsonElement accessTokenElement))
                    {
                        LoggerService.Log("ERREUR: Propriété 'access_token' non trouvée dans la réponse");
                        throw new Exception("Le token d'accès est manquant dans la réponse");
                    }

                    token = accessTokenElement.GetString();
                    if (string.IsNullOrEmpty(token))
                    {
                        LoggerService.Log("ERREUR: Token d'accès vide");
                        throw new Exception("Token d'accès vide reçu");
                    }

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

                    LoggerService.Log($"Authentification OAuth2 réussie pour {api.Name}");
                    LoggerService.Log($"Nouveau token valide jusqu'à : {_tokenExpiryTimes[api.ApiId]}");
                    LoggerService.Log("-------- FIN DE L'AUTHENTIFICATION --------");
                }
                catch (Exception ex)
                {
                    LoggerService.LogException(ex, $"Erreur lors de l'authentification OAuth2 pour {api.Name}");
                    throw;
                }
            }
            else if (api.AuthType == AuthenticationType.ApiKey)
            {
                LoggerService.Log("Authentification par clé API");

                if (api.AuthParameters.TryGetValue("HeaderName", out string headerName) &&
                    api.AuthParameters.TryGetValue("Value", out string value))
                {
                    client.DefaultRequestHeaders.Add(headerName, value);
                    LoggerService.Log($"En-tête {headerName} ajouté à la requête");
                }
                else
                {
                    LoggerService.Log("AVERTISSEMENT: Paramètres de clé API incomplets");
                }

                LoggerService.Log("-------- FIN DE L'AUTHENTIFICATION --------");
            }
            else if (api.AuthType == AuthenticationType.Basic)
            {
                LoggerService.Log("Authentification Basic");

                if (api.AuthParameters.TryGetValue("Username", out string username) &&
                    api.AuthParameters.TryGetValue("Password", out string password))
                {
                    var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
                    LoggerService.Log("En-tête d'authentification Basic ajouté");
                }
                else
                {
                    LoggerService.Log("AVERTISSEMENT: Paramètres d'authentification Basic incomplets");
                }

                LoggerService.Log("-------- FIN DE L'AUTHENTIFICATION --------");
            }
            else
            {
                LoggerService.Log($"Type d'authentification non géré : {api.AuthType}");
                LoggerService.Log("-------- FIN DE L'AUTHENTIFICATION --------");
            }
        }

        public async Task<string> TestApiConnectionAsync(string apiId, string endpointName)
        {
            try
            {
                LoggerService.Log($"Test de connexion pour API: {apiId}, Endpoint: {endpointName}");

                var api = _configuration.GetApiById(apiId);
                if (api == null)
                    throw new ArgumentException($"API non trouvée: {apiId}");

                var endpoint = api.Endpoints.Find(e => e.Name == endpointName);
                if (endpoint == null)
                    throw new ArgumentException($"Endpoint non trouvé: {endpointName}");

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

                // Construire l'URL complète
                string url = endpoint.Path;

                // Ajouter des paramètres si nécessaire
                var queryParams = new List<string>
        {
            "cross-company=true",
            "$top=10"
        };

                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                LoggerService.Log($"URL de test : {url}");

                // Exécuter la requête
                var response = await client.GetAsync(url);

                // Lire le contenu
                var content = await response.Content.ReadAsStringAsync();

                LoggerService.Log($"Statut de la réponse : {response.StatusCode}");
                LoggerService.Log($"Type de contenu : {response.Content.Headers.ContentType}");
                LoggerService.Log($"Longueur du contenu : {content.Length} caractères");
                LoggerService.Log($"Début du contenu (500 premiers caractères) :\n{content.Substring(0, Math.Min(500, content.Length))}");

                // Vérifier le succès de la requête
                response.EnsureSuccessStatusCode();

                return content;
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, "Erreur lors du test de connexion API");
                throw;
            }
        }

        private async Task<List<Dictionary<string, object>>> FetchDataFromDynamicsAsync(
    ApiConfiguration api,
    ApiEndpoint endpoint,
    DateTime? startDate,
    DateTime? endDate)
        {
            LoggerService.Log("-------- TRAITEMENT SPÉCIAL DYNAMICS 365 --------");

            try
            {
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

                // Construction de l'URL avec la syntaxe correcte pour Dynamics OData
                string baseUrl = endpoint.Path;

                // Construction de l'URL de base avec cross-company
                string url = $"{baseUrl}?cross-company=true";

                // Construire les filtres de date selon le format Dynamics
                if (startDate.HasValue && endDate.HasValue)
                {
                    string dateFilter = $"$filter=PurchasePriceDate ge {startDate.Value:yyyy-MM-dd}T00:00:00Z and " +
                                        $"PurchasePriceDate le {endDate.Value:yyyy-MM-dd}T23:59:59Z";

                    url += $"&{dateFilter}";
                    LoggerService.Log($"Filtre de date ajouté: {dateFilter}");
                }

                // Ajouter la limite d'enregistrements
                int maxRecords = _configuration.MaxRecords > 0 ? _configuration.MaxRecords : 500;
                url += $"&$top={maxRecords}";

                LoggerService.Log($"URL Dynamics OData complète: {url}");

                // Préparation des en-têtes
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Journalisation des en-têtes
                LoggerService.Log("En-têtes de la requête :");
                foreach (var header in client.DefaultRequestHeaders)
                {
                    LoggerService.Log($"{header.Key}: {string.Join(", ", header.Value)}");
                }

                // Exécution de la requête
                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync(url);
                }
                catch (Exception ex)
                {
                    LoggerService.Log($"ERREUR lors de l'appel API Dynamics : {ex.Message}");
                    LoggerService.LogException(ex, "Erreur d'appel API Dynamics");
                    throw new Exception($"Erreur lors de l'appel à l'API Dynamics : {ex.Message}", ex);
                }

                // Lecture du contenu
                var content = await response.Content.ReadAsStringAsync();

                // Journalisation détaillée de la réponse
                LoggerService.Log($"Statut de la réponse : {response.StatusCode}");
                LoggerService.Log($"Type de contenu : {response.Content.Headers.ContentType}");
                LoggerService.Log($"Longueur du contenu : {content.Length} caractères");
                LoggerService.Log($"Début du contenu (500 premiers caractères) :\n{content.Substring(0, Math.Min(500, content.Length))}");

                // Vérification du succès de la requête
                if (!response.IsSuccessStatusCode)
                {
                    LoggerService.Log($"ERREUR de l'API Dynamics: {response.StatusCode} - {content}");
                    throw new Exception($"Erreur de l'API Dynamics: {response.StatusCode} - {content}");
                }

                // Options de désérialisation flexibles
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                // Désérialisation spécifique à Dynamics
                try
                {
                    using (var doc = JsonDocument.Parse(content))
                    {
                        if (doc.RootElement.TryGetProperty("value", out JsonElement valueElement))
                        {
                            var valueJson = valueElement.GetRawText();
                            LoggerService.Log($"Contenu de 'value' trouvé, longueur: {valueJson.Length}");

                            var items = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                                valueJson,
                                options
                            ) ?? new List<Dictionary<string, object>>();

                            LoggerService.Log($"Désérialisation réussie: {items.Count} éléments récupérés");

                            // Ajouter des logs pour examiner les données
                            if (items.Count > 0)
                            {
                                // Échantillon du premier élément pour débogage
                                var firstItem = items[0];
                                LoggerService.Log("Premier élément récupéré (échantillon) :");
                                foreach (var kvp in firstItem.Take(5)) // Limiter à 5 champs pour éviter des logs trop volumineux
                                {
                                    LoggerService.Log($"  - {kvp.Key}: {kvp.Value}");
                                }
                            }
                            else
                            {
                                LoggerService.Log("ATTENTION : Aucun élément récupéré de l'API !");
                            }

                            LoggerService.Log("-------- FIN DU TRAITEMENT DYNAMICS 365 --------");
                            return items;
                        }
                        else
                        {
                            LoggerService.Log("ERREUR : Propriété 'value' non trouvée dans la réponse JSON Dynamics");
                            throw new Exception("Format de réponse Dynamics invalide: propriété 'value' non trouvée");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    LoggerService.LogException(ex, "Erreur lors de la désérialisation JSON Dynamics");
                    LoggerService.Log($"Contenu problématique :\n{content}");
                    throw new Exception($"Erreur de désérialisation JSON Dynamics: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, "Erreur globale lors du traitement Dynamics");
                throw;
            }
        }
    }
}