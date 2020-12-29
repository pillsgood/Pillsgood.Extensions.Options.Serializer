using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Pillsgood.Extensions.Options.Serializer
{
    internal class OptionWriter
    {
        private readonly IEnumerable<FileConfigurationSource> _providerSources;

        public OptionWriter(IConfiguration configuration)
        {
            _providerSources = ((IConfigurationRoot) configuration).Providers.OfType<JsonConfigurationProvider>()
                .Select(provider => provider.Source);
        }

        internal void Update<T>(IConfiguration configuration, Action<T> applyChanges)
        {
            switch (configuration)
            {
                case IConfigurationSection section:
                    Update(section, applyChanges);
                    break;
                case IConfigurationRoot root:
                    Update(root, applyChanges);
                    break;
            }
        }

        private void Update<T>(IConfigurationSection configurationSection, Action<T> applyChanges)
        {
            Update(applyChanges, configurationSection.Key);
        }

        private void Update<T>(IConfigurationRoot configuration, Action<T> applyChanges)
        {
            Update(applyChanges, configuration.GetSection(typeof(T).Name).Key);
        }

        private void Update<T>(Action<T> applyChanges, string key)
        {
            var jObjects = _providerSources.Select(source => source.FileProvider.GetFileInfo(source.Path))
                .Where(info => info.Exists)
                .ToDictionary(info => info.PhysicalPath,
                    info => JsonConvert.DeserializeObject<JObject>(File.ReadAllText(info.PhysicalPath)));
            var anyExists = jObjects.Any(pair => pair.Value.ContainsKey(key));
            var (path, jObject) = anyExists
                ? jObjects.First(pair => pair.Value.ContainsKey(key))
                : jObjects.First();
            var section = anyExists && jObject[key] != null
                ? JsonConvert.DeserializeObject<T>(jObject[key].ToString())
                : JsonConvert.DeserializeObject<T>("{}", new JsonSerializerSettings
                {
                    DefaultValueHandling = DefaultValueHandling.Populate
                });
            applyChanges?.Invoke(section);
            jObject[key] = JObject.Parse(JsonConvert.SerializeObject(section));
            File.WriteAllText(path, JsonConvert.SerializeObject(jObject, Formatting.Indented));
        }
    }
}