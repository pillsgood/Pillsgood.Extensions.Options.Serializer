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
            foreach (var providerSource in _providerSources)
            {
                Update(configurationSection, applyChanges, providerSource);
            }
        }

        private void Update<T>(IConfigurationRoot configuration, Action<T> applyChanges)
        {
            foreach (var providerSource in _providerSources)
            {
                Update(configuration.GetSection(typeof(T).Name), applyChanges, providerSource);
            }
        }

        internal void Update<T>(IConfigurationSection section, Action<T> applyChanges,
            FileConfigurationSource configurationSource)
        {
            var fileInfo = configurationSource.FileProvider.GetFileInfo(configurationSource.Path);
            if (!fileInfo.Exists)
            {
                return;
            }

            var path = fileInfo.PhysicalPath;
            var jObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(path));
            var sectionObject = jObject.TryGetValue(section.Key, out var sectionToken)
                ? JsonConvert.DeserializeObject<T>(sectionToken.ToString())
                : JsonConvert.DeserializeObject<T>("{}", new JsonSerializerSettings
                {
                    DefaultValueHandling = DefaultValueHandling.Populate
                });
            applyChanges?.Invoke(sectionObject);
            jObject[section.Key] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));
            File.WriteAllText(path, JsonConvert.SerializeObject(jObject, Formatting.Indented));
        }
    }
}