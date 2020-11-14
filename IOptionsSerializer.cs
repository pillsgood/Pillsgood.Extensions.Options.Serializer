using System;
using Microsoft.Extensions.Options;

namespace Pillsgood.Extensions.Options.Serializer
{
    public interface IOptionsSerializer<out T> : IOptionsMonitor<T> where T : class
    {
        public void Serialize(Action<T> changes);
    }
}