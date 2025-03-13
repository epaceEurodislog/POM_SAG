using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;
using POMsag.Models;

namespace POMsag.Services
{
    public class SchemaAnalysisService
    {
        private IDynamicsApiService _dynamicsApiService;
        private readonly HttpClient _httpClient;
        private readonly AppConfiguration _configuration;

        public SchemaAnalysisService(DynamicsApiService dynamicsApiService, HttpClient httpClient, AppConfiguration configuration)
        {
            _dynamicsApiService = dynamicsApiService;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<HashSet<string>> DiscoverFieldsFromDynamics(string entity, int sampleSize = 1)
        {
            try
            {
                var fields = new HashSet<string>();

                // Pour ReleasedProductsV2
                if (entity == "ReleasedProductsV2")
                {
                    var products = await _dynamicsApiService.GetReleasedProductsAsync(null, null);

                    // Limiter au nombre d'échantillons demandés
                    int count = Math.Min(sampleSize, products.Count);

                    for (int i = 0; i < count; i++)
                    {
                        var product = products[i];

                        // Ajouter les propriétés standard
                        var propertyInfos = typeof(ReleasedProduct).GetProperties();
                        foreach (var prop in propertyInfos)
                        {
                            if (prop.Name != "AdditionalProperties")
                                fields.Add(prop.Name);
                        }

                        // Ajouter les propriétés additionnelles
                        foreach (var prop in product.AdditionalProperties)
                        {
                            fields.Add(prop.Key);
                        }

                        // Ajouter les propriétés d'un objet Dictionary
                        var dict = product.ToDictionary();
                        foreach (var key in dict.Keys)
                        {
                            fields.Add(key);
                        }
                    }
                }
                return fields;
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"Découverte des champs pour {entity}");
                throw;
            }
        }

        public async Task<HashSet<string>> DiscoverFieldsFromPomApi(string endpoint, int sampleSize = 1)
        {
            try
            {
                var fields = new HashSet<string>();

                // Ajouter en-tête d'authentification si nécessaire
                if (!_httpClient.DefaultRequestHeaders.Contains("X-Api-Key"))
                    _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _configuration.ApiKey);

                // Construire l'URL avec limite
                string url = $"api/{endpoint}?limit={sampleSize}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                // Désérialiser en JsonElement pour analyser dynamiquement
                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    JsonElement root = doc.RootElement;

                    // Si c'est un tableau, prendre le premier élément
                    if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    {
                        var firstItem = root[0];

                        // Extraire toutes les propriétés
                        foreach (var property in firstItem.EnumerateObject())
                        {
                            fields.Add(property.Name);
                        }
                    }
                }

                return fields;
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"Découverte des champs pour {endpoint}");
                throw;
            }
        }

        public async Task<HashSet<string>> DiscoverFields(string sourceType, string entity)
        {
            if (sourceType.ToLower() == "dynamics")
                return await DiscoverFieldsFromDynamics(entity);
            else // POM API
                return await DiscoverFieldsFromPomApi(entity);
        }
    }
}