#nullable enable
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Pillsgood.Extensions.Options.Serializer
{
    public static class OptionsConfigurationServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureWithSerializer<T>(this IServiceCollection services,
            IConfiguration configuration, IFileProvider fileProvider) where T : class =>
            services.ConfigureWithSerializer<T>(Microsoft.Extensions.Options.Options.DefaultName, configuration,
                fileProvider);

        public static IServiceCollection ConfigureWithSerializer<T>(this IServiceCollection services,
            string name, IConfiguration configuration, IFileProvider fileProvider) where T : class =>
            services.ConfigureWithSerializer<T>(name, configuration, _ => { }, fileProvider);

        public static IServiceCollection ConfigureWithSerializer<TOptions>(this IServiceCollection services,
            IConfiguration config, IFileProvider fileProvider, Action<BinderOptions> configureBinder)
            where TOptions : class
            => services.ConfigureWithSerializer<TOptions>(Microsoft.Extensions.Options.Options.DefaultName, config,
                configureBinder, fileProvider);

        public static IServiceCollection ConfigureWithSerializer<T>(this IServiceCollection services,
            IConfiguration configuration) where T : class =>
            services.ConfigureWithSerializer<T>(Microsoft.Extensions.Options.Options.DefaultName, configuration);

        public static IServiceCollection ConfigureWithSerializer<T>(this IServiceCollection services,
            string name, IConfiguration configuration) where T : class =>
            services.ConfigureWithSerializer<T>(name, configuration, _ => { });

        public static IServiceCollection ConfigureWithSerializer<TOptions>(this IServiceCollection services,
            IConfiguration config, Action<BinderOptions> configureBinder)
            where TOptions : class
            => services.ConfigureWithSerializer<TOptions>(Microsoft.Extensions.Options.Options.DefaultName, config,
                configureBinder);

        public static IServiceCollection ConfigureWithSerializer<T>(this IServiceCollection services,
            string name, IConfiguration configuration, Action<BinderOptions> configureBinder,
            IFileProvider? fileProvider = null)
            where T : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.AddOptions();
            services.TryAddSingleton<OptionWriter>();
            services.AddSingleton<IOptionsChangeTokenSource<T>>(
                new ConfigurationChangeTokenSource<T>(name, configuration));
            services.AddSingleton<IConfigureOptions<T>>(
                new NamedConfigureFromConfigurationOptions<T>(name, configuration, configureBinder));
            return services.AddTransient<IOptionsSerializer<T>>(provider =>
                ActivatorUtilities.CreateInstance<OptionsSerializer<T>>(provider, name, configuration, fileProvider!));
        }
    }
}