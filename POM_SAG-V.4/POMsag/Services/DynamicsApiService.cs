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
    public class DynamicsApiService
    {
        private readonly HttpClient _httpClient;
        private string _accessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;

        // Configuration des paramètres d'authentification
        private readonly string _tokenUrl;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _resource;
        private readonly string _dynamicsApiUrl;
        private readonly int _maxRecords;

        public DynamicsApiService(string tokenUrl, string clientId, string clientSecret, string resource, string dynamicsApiUrl, int maxRecords)
        {
            _httpClient = new HttpClient();
            _tokenUrl = tokenUrl;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _resource = resource;
            _dynamicsApiUrl = dynamicsApiUrl;
            _maxRecords = maxRecords;
        }

        public async Task<string> GetTokenAsync()
        {
            // Si le token est toujours valide, le réutiliser
            if (_accessToken != null && DateTime.Now < _tokenExpiry)
            {
                return _accessToken;
            }

            LoggerService.Log("Demande d'un nouveau token OAuth");

            try
            {
                // Vérifier que l'URL du token est valide
                if (string.IsNullOrEmpty(_tokenUrl))
                {
                    throw new ArgumentException("L'URL du token ne peut pas être nulle ou vide");
                }

                // S'assurer que l'URL est absolue
                if (!Uri.IsWellFormedUriString(_tokenUrl, UriKind.Absolute))
                {
                    throw new ArgumentException($"L'URL du token '{_tokenUrl}' n'est pas une URL absolue valide");
                }

                // Préparer la requête pour obtenir le token
                var requestContent = new FormUrlEncodedContent(new[]
                {
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("resource", _resource)
        });

                // Utiliser un HttpClientHandler pour ignorer les erreurs de certificat
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                // Envoyer la requête
                using (var client = new HttpClient(handler))
                {
                    var response = await client.PostAsync(_tokenUrl, requestContent);
                    response.EnsureSuccessStatusCode();

                    // Traiter la réponse
                    var responseContent = await response.Content.ReadAsStringAsync();
                    LoggerService.Log($"Réponse du serveur OAuth reçue: {responseContent.Substring(0, Math.Min(50, responseContent.Length))}...");

                    var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    // Extraction sécurisée des informations du token
                    if (!tokenResponse.TryGetProperty("access_token", out JsonElement accessTokenElement))
                    {
                        throw new Exception("La propriété 'access_token' est absente de la réponse");
                    }
                    _accessToken = accessTokenElement.GetString();

                    // Traiter le champ "expires_in" qui peut être un nombre ou une chaîne
                    int expiresIn;
                    if (tokenResponse.TryGetProperty("expires_in", out JsonElement expiresInProp))
                    {
                        if (expiresInProp.ValueKind == JsonValueKind.String)
                        {
                            // Si c'est une chaîne, la convertir en entier
                            if (int.TryParse(expiresInProp.GetString(), out int result))
                                expiresIn = result;
                            else
                                expiresIn = 3599; // Valeur par défaut d'une heure moins une seconde
                        }
                        else
                        {
                            // Si c'est déjà un nombre
                            expiresIn = expiresInProp.GetInt32();
                        }
                    }
                    else
                    {
                        expiresIn = 3599; // Valeur par défaut si la propriété est absente
                    }

                    _tokenExpiry = DateTime.Now.AddSeconds(expiresIn - 60); // Marge de sécurité d'une minute

                    LoggerService.Log($"Token OAuth obtenu, valide jusqu'à {_tokenExpiry}");
                    return _accessToken;
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, "Obtention du token OAuth");
                throw new Exception("Erreur lors de l'obtention du token d'authentification", ex);
            }
        }

        public async Task<string> GetDataAsync(string endpoint, Dictionary<string, string> filters = null)
        {
            // Construire l'URL avec les filtres OData si nécessaire
            var url = $"{_dynamicsApiUrl}/{endpoint}";

            if (filters != null && filters.Count > 0)
            {
                var oDataFilter = BuildODataFilter(filters);
                url += oDataFilter;
            }

            LoggerService.Log($"Préparation requête GET vers: {url}");

            try
            {
                // Obtenir un token valide
                var token = await GetTokenAsync();

                // Préparer la requête avec le token
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Envoyer la requête
                LoggerService.Log("Envoi de la requête GET");
                var response = await _httpClient.SendAsync(request);

                var content = await response.Content.ReadAsStringAsync();
                LoggerService.Log($"Réponse reçue, statut: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    LoggerService.Log($"Erreur API: {response.StatusCode} - {content}");
                    throw new Exception($"Erreur de l'API Dynamics: {response.StatusCode} - {content}");
                }

                // Retourner le contenu de la réponse
                return content;
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"GetDataAsync pour {endpoint}");
                throw;
            }
        }

        private string BuildODataFilter(Dictionary<string, string> filters)
        {
            if (filters == null || filters.Count == 0)
                return string.Empty;

            var filterParts = new List<string>();
            bool hasQueryParam = false;

            foreach (var filter in filters)
            {
                if (string.IsNullOrEmpty(filter.Key) || string.IsNullOrEmpty(filter.Value))
                    continue;

                if (!hasQueryParam)
                {
                    hasQueryParam = true;
                    filterParts.Add("?$filter=");
                }
                else
                {
                    filterParts.Add(" and ");
                }

                filterParts.Add($"{filter.Key} {filter.Value}");
            }

            return string.Concat(filterParts);
        }

        public async Task<List<ReleasedProduct>> GetReleasedProductsAsync(
            DateTime? startPurchaseDate = null,
            DateTime? endPurchaseDate = null)
        {
            try
            {
                // Construction de l'URL de base sans limite fixe
                var baseUrl = $"ReleasedProductsV2?cross-company=true";

                // Construire la partie filtre
                var filterParts = new List<string>();

                // Filtre par date d'achat
                if (startPurchaseDate.HasValue && endPurchaseDate.HasValue)
                {
                    filterParts.Add($"PurchasePriceDate ge {startPurchaseDate.Value:yyyy-MM-dd}T00:00:00Z and PurchasePriceDate le {endPurchaseDate.Value:yyyy-MM-dd}T23:59:59Z");
                }

                // Ajouter les filtres à l'URL
                if (filterParts.Count > 0)
                {
                    baseUrl += "&$filter=" + string.Join(" and ", filterParts);
                }

                // Ajouter la limite dynamique
                baseUrl += $"&$top={_maxRecords}";

                LoggerService.Log($"URL de requête OData: {baseUrl}");

                var jsonContent = await GetDataAsync(baseUrl);

                // Désérialisation
                using (var document = JsonDocument.Parse(jsonContent))
                {
                    if (document.RootElement.TryGetProperty("value", out var valueElement))
                    {
                        var products = JsonSerializer.Deserialize<List<ReleasedProduct>>(
                            valueElement.GetRawText(),
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            }
                        );

                        return products ?? new List<ReleasedProduct>();
                    }

                    return new List<ReleasedProduct>();
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, "GetReleasedProductsAsync");
                throw new Exception("Erreur lors de la récupération des produits depuis Dynamics 365", ex);
            }
        }
    }
}