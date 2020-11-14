using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Pillsgood.Extensions.Options.Serializer
{
    internal class OptionsSerializer<T> : IOptionsSerializer<T> where T : class
    {
        private readonly IOptionsMonitor<T> _optionsMonitor;
        private readonly OptionWriter _optionWriter;
        private readonly IConfigurationSection _configurationSection;

        public OptionsSerializer(IOptionsMonitor<T> optionsMonitor, OptionWriter optionWriter,
            IConfigurationSection configurationSection)
        {
            _optionsMonitor = optionsMonitor;
            _optionWriter = optionWriter;
            _configurationSection = configurationSection;
        }

        public void Serialize(Action<T> changes) => _optionWriter.Update(_configurationSection, changes);

        public T Get(string name) => _optionsMonitor.Get(name);

        public IDisposable OnChange(Action<T, string> listener) => _optionsMonitor.OnChange(listener);

        public T CurrentValue => _optionsMonitor.CurrentValue;
    }
}