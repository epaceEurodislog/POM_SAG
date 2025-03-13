using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using POMsag.Models;
#pragma warning disable CS8618, CS8625, CS8600, CS8602, CS8603, CS8604, CS8601

namespace POMsag.Services
{
    public class GenericApiService
    {
        private readonly AppConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private IDynamicsApiService _dynamicsApiService;

        public GenericApiService(AppConfiguration configuration, HttpClient? httpClient = null, DynamicsApiService? dynamicsApiService = null)
        {
            _configuration = configuration;
            _httpClient = httpClient ?? new HttpClient
            {
                BaseAddress = new Uri(_configuration.ApiUrl ?? "http://localhost:5001/")
            };

            // Ajouter la clé API si disponible
            if (!string.IsNullOrEmpty(_configuration.ApiKey) && httpClient == null)
            {
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _configuration.ApiKey);
            }

            _dynamicsApiService = dynamicsApiService;
        }

        public async Task<List<Dictionary<string, object>>> FetchDataAsync(
            string source,
            string endpointName,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            LoggerService.Log($"FetchDataAsync - Source: {source}, Endpoint: {endpointName}");

            // Si la source est Dynamics 365
            if (source.ToLower() == "dynamics")
            {
                return await FetchDataFromDynamicsAsync(endpointName, startDate, endDate);
            }
            // Sinon, utiliser l'API POM standard
            else
            {
                return await FetchDataFromPomApiAsync(endpointName, startDate, endDate);
            }
        }

        private async Task<List<Dictionary<string, object>>> FetchDataFromPomApiAsync(
            string endpoint,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                string url = $"api/{endpoint.ToLower()}";

                // Ajouter les paramètres de date si nécessaire
                if (startDate.HasValue && endDate.HasValue)
                {
                    var startDateStr = startDate.Value.ToString("yyyyMMdd");
                    var endDateStr = endDate.Value.ToString("yyyyMMdd");
                    url += $"?startDate={startDateStr}&endDate={endDateStr}";
                }

                LoggerService.Log($"Appel API POM: {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                LoggerService.Log($"Réponse reçue, {content.Length} caractères");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                    content,
                    options
                );

                if (result == null)
                {
                    throw new Exception("La désérialisation a retourné null");
                }

                LoggerService.Log($"Données récupérées: {result.Count} éléments");
                return result;
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"FetchDataFromPomApiAsync - {endpoint}");
                throw;
            }
        }

        private async Task<List<Dictionary<string, object>>> FetchDataFromDynamicsAsync(
            string endpoint,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                LoggerService.Log($"Récupération des données Dynamics - Endpoint: {endpoint}");

                if (endpoint == "ReleasedProductsV2")
                {
                    var products = await _dynamicsApiService.GetReleasedProductsAsync(
                        startDate,
                        endDate
                    );

                    LoggerService.Log($"Produits récupérés: {products.Count}");

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
                    throw new NotImplementedException($"L'endpoint Dynamics '{endpoint}' n'est pas implémenté");
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"FetchDataFromDynamicsAsync - {endpoint}");
                throw;
            }
        }
    }
}