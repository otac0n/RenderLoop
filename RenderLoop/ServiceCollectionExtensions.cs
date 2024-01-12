namespace RenderLoop
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddKeyedSingleton<TService, TKey>(
            this IServiceCollection services,
            TKey serviceKey,
            Func<IServiceProvider, TKey, TService> implementationFactory)
            where TService : class =>
            services.AddKeyedSingleton<TService>(serviceKey, (IServiceProvider s, object? key) => implementationFactory(s, (TKey)key!));
    }
}
