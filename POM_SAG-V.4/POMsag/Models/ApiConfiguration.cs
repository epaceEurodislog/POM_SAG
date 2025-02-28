// Fichier: POMsag/Models/ApiConfiguration.cs
using System.Collections.Generic;
using System;

namespace POMsag.Models
{
    [Serializable]
    public class ApiConfiguration
    {
        public string ApiId { get; set; }
        public string Name { get; set; }
        public string BaseUrl { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public List<ApiEndpoint> Endpoints { get; set; } = new List<ApiEndpoint>();
        public AuthenticationType AuthType { get; set; } = AuthenticationType.None;
        public Dictionary<string, string> AuthParameters { get; set; } = new Dictionary<string, string>();

        public ApiConfiguration(string apiId, string name, string baseUrl)
        {
            ApiId = apiId;
            Name = name;
            BaseUrl = baseUrl;
        }
    }

    [Serializable]
    public class ApiEndpoint
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Method { get; set; } = "GET";
        public bool SupportsDateFiltering { get; set; }
        public string DateStartParamName { get; set; }
        public string DateEndParamName { get; set; }
        public string DateFormat { get; set; } = "yyyyMMdd";

        public ApiEndpoint(string name, string path)
        {
            Name = name;
            Path = path;
        }
    }

    public enum AuthenticationType
    {
        None,
        ApiKey,
        OAuth2ClientCredentials,
        Basic,
        Custom
    }
}