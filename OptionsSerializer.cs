using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Pillsgood.Extensions.Options.Serializer
{
    internal class OptionsSerializer<T> : IOptionsSerializer<T> where T : class
    {
        private readonly IFileProvider _fileProvider;
        private readonly IOptionsMonitor<T> _optionsMonitor;
        private readonly OptionWriter _optionWriter;
        private readonly string _key;

        public OptionsSerializer(IOptionsMonitor<T> optionsMonitor, OptionWriter optionWriter,
            string name, IConfiguration configuration, IFileProvider fileProvider) : this(optionsMonitor, optionWriter,
            name, configuration)
        {
            _fileProvider = fileProvider;
        }

        public OptionsSerializer(IOptionsMonitor<T> optionsMonitor, OptionWriter optionWriter,
            string name, IConfiguration configuration)
        {
            _optionsMonitor = optionsMonitor;
            _optionWriter = optionWriter;
            _key = string.Join(':', new[]
            {
                name, configuration switch
                {
                    IConfigurationSection section => section.Key,
                    IConfigurationRoot _ => string.Empty,
                    _ => throw new InvalidOperationException()
                }
            }.Where(s => !string.IsNullOrEmpty(s)));
        }


        public void Serialize(Action<T> changes) => _optionWriter.Update(_key, changes, _fileProvider);

        public T Get(string name) => _optionsMonitor.Get(name);

        public IDisposable OnChange(Action<T, string> listener) => _optionsMonitor.OnChange(listener);

        public T CurrentValue => _optionsMonitor.CurrentValue;
    }
}