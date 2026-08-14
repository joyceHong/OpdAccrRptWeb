using Microsoft.AspNetCore.Antiforgery;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace OpdAccrRptWeb.Help
{
    public static class ModelDescriptionsHelper
    {
        public class PropertyMetadata
        {            
            public required string Key { get; set; }            
            
            public required string Label { get; set; }
        }

        private static readonly ConcurrentDictionary<Type, List<PropertyMetadata>> _cache = new();

        public static List<PropertyMetadata> GetPropertyDescriptions<T>()
        {
            return _cache.GetOrAdd(typeof(T), type =>
            {
                return type.GetProperties()
                    .Select(p => new PropertyMetadata
                    {
                        Key = JsonNamingPolicy.CamelCase.ConvertName(p.Name),
                        Label = p.GetCustomAttribute<DescriptionAttribute>()?.Description ?? p.Name
                    })
                    .ToList();
            });
        }
    }
}
