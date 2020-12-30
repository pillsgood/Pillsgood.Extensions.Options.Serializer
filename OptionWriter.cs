using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Pillsgood.Extensions.Options.Serializer
{
    internal class OptionWriter
    {
        private IConfiguration _configuration;

        private readonly IEnumerable<FileConfigurationSource> _providerSources;
        // private readonly Dictionary<Type, string> _optionsConfigurationSectionKeys = new();
        // private IChangeToken _reloadToken;

        public OptionWriter(IConfiguration configuration)
        {
            _configuration = configuration;
            // _reloadToken = _configuration.GetReloadToken();
            // ChangeToken.OnChange(configuration.GetReloadToken,
            //     config => _configuration = config,
            //     _configuration);
            _providerSources = ((IConfigurationRoot) configuration).Providers.OfType<JsonConfigurationProvider>()
                .Select(provider => provider.Source);
        }
        
        internal void Update<T>(string key, Action<T> applyChanges, IFileProvider fileProvider)
        {
            var section = DetermineConfigurationSource(key, fileProvider, out var path, out var jObject) &&
                          jObject[key] != null
                ? JsonConvert.DeserializeObject<T>(jObject[key].ToString())
                : JsonConvert.DeserializeObject<T>("{}", new JsonSerializerSettings
                {
                    DefaultValueHandling = DefaultValueHandling.Populate
                });
            applyChanges?.Invoke(section);
            jObject[key] = JObject.Parse(JsonConvert.SerializeObject(section));
            File.WriteAllText(path, JsonConvert.SerializeObject(jObject, Formatting.Indented));
        }

        private bool ConfigurationSourceFromProvider(string key, IFileProvider fileProvider, out string path,
            out JObject jObject, out bool sectionExists)
        {
            path = string.Empty;
            jObject = default;
            sectionExists = false;
            if (fileProvider == null) return false;
            var source = _providerSources.First(configurationSource =>
                configurationSource.FileProvider.Equals(fileProvider));
            var fileInfo = source.FileProvider.GetFileInfo(source.Path);
            path = fileInfo.PhysicalPath;
            jObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(path));
            sectionExists = jObject.ContainsKey(key);
            return true;
        }

        private bool DetermineConfigurationSource(string key, IFileProvider fileProvider, out string path,
            out JObject jObject)
        {
            if (ConfigurationSourceFromProvider(key, fileProvider, out path, out jObject, out var sectionExists))
                return sectionExists;

            var jObjects = _providerSources.Select(source => source.FileProvider.GetFileInfo(source.Path))
                .Where(info => info.Exists)
                .ToDictionary(info => info.PhysicalPath,
                    info => JsonConvert.DeserializeObject<JObject>(File.ReadAllText(info.PhysicalPath)));
            sectionExists = jObjects.Any(pair => pair.Value.ContainsKey(key));
            (path, jObject) = sectionExists
                ? jObjects.First(pair => pair.Value.ContainsKey(key))
                : jObjects.First();
            return sectionExists;
        }
    }
}