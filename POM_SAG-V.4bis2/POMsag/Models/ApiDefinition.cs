using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace POMsag.Models
{
    [Serializable]
    public class ApiDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string BaseUrl { get; set; }
        public ApiAuthType AuthType { get; set; } = ApiAuthType.None;
        public Dictionary<string, string> AuthProperties { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public List<ApiEndpoint> Endpoints { get; set; } = new List<ApiEndpoint>();
        public Dictionary<string, string> GlobalParameters { get; set; } = new Dictionary<string, string>();
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastModifiedDate { get; set; }

        public ApiDefinition()
        {
        }

        public ApiDefinition(string name, string baseUrl)
        {
            Name = name;
            BaseUrl = baseUrl;
        }

        // Méthode pour valider la définition de l'API
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Le nom de l'API est requis.");

            if (string.IsNullOrWhiteSpace(BaseUrl))
                errors.Add("L'URL de base de l'API est requise.");

            if (!Uri.IsWellFormedUriString(BaseUrl, UriKind.Absolute))
                errors.Add("L'URL de base doit être une URL valide.");

            if (Endpoints == null || Endpoints.Count == 0)
                errors.Add("Au moins un endpoint doit être défini.");
            else
            {
                foreach (var endpoint in Endpoints)
                {
                    if (string.IsNullOrWhiteSpace(endpoint.Name))
                        errors.Add($"Un endpoint sans nom a été trouvé.");

                    if (string.IsNullOrWhiteSpace(endpoint.Path))
                        errors.Add($"Le chemin du endpoint '{endpoint.Name}' est requis.");
                }
            }

            return errors.Count == 0;
        }
    }

    public enum ApiAuthType
    {
        None,
        ApiKey,
        OAuth2,
        Basic,
        Bearer,
        Custom
    }
}