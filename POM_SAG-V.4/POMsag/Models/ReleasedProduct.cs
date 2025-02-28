using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace POMsag.Models
{
    public class ReleasedProduct
    {
        // Dictionnaire pour stocker toutes les propriétés
        [JsonExtensionData]
        public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();

        [JsonPropertyName("@odata.etag")]
        public string ODataEtag { get; set; }

        [JsonPropertyName("dataAreaId")]
        public string DataAreaId { get; set; }

        [JsonPropertyName("ItemNumber")]
        public string ItemNumber { get; set; }

        [JsonPropertyName("IsPhantom")]
        public string IsPhantom { get; set; }

        [JsonPropertyName("IsPurchasePriceIncludingCharges")]
        public string IsPurchasePriceIncludingCharges { get; set; }

        [JsonPropertyName("ItemFiscalClassificationCode")]
        public string ItemFiscalClassificationCode { get; set; }

        [JsonPropertyName("SeventhProductFilterCode")]
        public string SeventhProductFilterCode { get; set; }

        [JsonPropertyName("ServiceAccountingCode")]
        public string ServiceAccountingCode { get; set; }

        [JsonPropertyName("MarginABCCode")]
        public string MarginABCCode { get; set; }

        [JsonPropertyName("IsICMSTaxAppliedOnService")]
        public string IsICMSTaxAppliedOnService { get; set; }

        // Les autres propriétés importantes
        [JsonPropertyName("ProductNumber")]
        public string ProductNumber { get; set; }

        [JsonPropertyName("ProductName")]
        public string ProductName { get; set; }

        [JsonPropertyName("ProductDescription")]
        public string ProductDescription { get; set; }

        [JsonPropertyName("ProductType")]
        public string ProductType { get; set; }

        [JsonPropertyName("ProductSubType")]
        public string ProductSubType { get; set; }

        // Méthode pour obtenir tous les détails
        public string GetFullDetailsAsJson()
        {
            // Création d'un dictionnaire avec un ordre spécifique
            var orderedProperties = new Dictionary<string, object>
            {
                { "@odata.etag", ODataEtag },
                { "dataAreaId", DataAreaId },
                { "ItemNumber", ItemNumber },
                { "IsPhantom", IsPhantom },
                { "IsPurchasePriceIncludingCharges", IsPurchasePriceIncludingCharges },
                { "ItemFiscalClassificationCode", ItemFiscalClassificationCode },
                { "SeventhProductFilterCode", SeventhProductFilterCode },
                { "ServiceAccountingCode", ServiceAccountingCode },
                { "MarginABCCode", MarginABCCode },
                { "IsICMSTaxAppliedOnService", IsICMSTaxAppliedOnService },
                { "ProductNumber", ProductNumber },
                { "ProductName", ProductName },
                { "ProductDescription", ProductDescription },
                { "ProductType", ProductType },
                { "ProductSubType", ProductSubType }
            };

            // Ajouter toutes les propriétés additionnelles
            foreach (var prop in AdditionalProperties)
            {
                orderedProperties[prop.Key] = prop.Value;
            }

            return JsonSerializer.Serialize(orderedProperties,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                });
        }

        // Conversion en dictionnaire pour faciliter le transfert avec un ordre spécifique
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                { "@odata.etag", ODataEtag },
                { "dataAreaId", DataAreaId },
                { "ItemNumber", ItemNumber },
                { "IsPhantom", IsPhantom },
                { "IsPurchasePriceIncludingCharges", IsPurchasePriceIncludingCharges },
                { "ItemFiscalClassificationCode", ItemFiscalClassificationCode },
                { "SeventhProductFilterCode", SeventhProductFilterCode },
                { "ServiceAccountingCode", ServiceAccountingCode },
                { "MarginABCCode", MarginABCCode },
                { "IsICMSTaxAppliedOnService", IsICMSTaxAppliedOnService },
                { "ProductNumber", ProductNumber },
                { "ProductName", ProductName },
                { "ProductDescription", ProductDescription },
                { "ProductType", ProductType },
                { "ProductSubType", ProductSubType }
            };

            // Ajouter toutes les propriétés additionnelles à la fin
            foreach (var prop in AdditionalProperties)
            {
                dict[prop.Key] = prop.Value;
            }

            return dict;
        }

        // Autres méthodes restent identiques
        public int GetTotalPropertiesCount()
        {
            return 15 + AdditionalProperties.Count;
        }

        public bool HasProperty(string propertyName)
        {
            if (propertyName == "@odata.etag") return ODataEtag != null;
            if (propertyName == "dataAreaId") return DataAreaId != null;
            if (propertyName == "ItemNumber") return ItemNumber != null;
            // Ajoutez des vérifications similaires pour d'autres propriétés principales

            return AdditionalProperties.ContainsKey(propertyName);
        }

        public object GetPropertyValue(string propertyName)
        {
            switch (propertyName)
            {
                case "@odata.etag": return ODataEtag;
                case "dataAreaId": return DataAreaId;
                case "ItemNumber": return ItemNumber;
                case "ProductNumber": return ProductNumber;
                    // Ajoutez d'autres cas pour les propriétés principales
            }

            if (AdditionalProperties.TryGetValue(propertyName, out object value))
            {
                return value;
            }

            return null;
        }
    }
}