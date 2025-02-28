using System;
using System.Collections.Generic;

namespace POMsag.Models
{
    [Serializable]
    public class FieldSelectionPreference
    {
        public string EntityName { get; set; }
        public Dictionary<string, bool> Fields { get; set; } = new Dictionary<string, bool>();

        public FieldSelectionPreference(string entityName)
        {
            EntityName = entityName;
        }

        public void AddOrUpdateField(string fieldName, bool isSelected = true)
        {
            if (Fields.ContainsKey(fieldName))
                Fields[fieldName] = isSelected;
            else
                Fields.Add(fieldName, isSelected);
        }
    }
}