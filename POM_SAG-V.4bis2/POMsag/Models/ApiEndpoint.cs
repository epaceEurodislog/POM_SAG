using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace POMsag.Models
{
    [Serializable]
    public class ApiEndpoint
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }
        public HttpMethod Method { get; set; } = HttpMethod.Get;
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        public bool SupportsDateFiltering { get; set; }
        public string StartDateParamName { get; set; }
        public string EndDateParamName { get; set; }
        public string DateFormat { get; set; } = "yyyy-MM-dd";
        public string ResponseRootPath { get; set; } = "value"; // Chemin JSON pour extraire les données (ex: "data.items")
        public Dictionary<string, string> FieldMappings { get; set; } = new Dictionary<string, string>();

        public ApiEndpoint()
        {
        }

        public ApiEndpoint(string name, string path, HttpMethod method = HttpMethod.Get)
        {
            Name = name;
            Path = path;
            Method = method;
        }

        // Pour stocker les métadonnées enrichies au moment de l'exécution (non sérialisées)
        [JsonIgnore]
        public Dictionary<string, object> RuntimeMetadata { get; set; } = new Dictionary<string, object>();
    }

    public enum HttpMethod
    {
        Get,
        Post,
        Put,
        Delete,
        Patch
    }
}