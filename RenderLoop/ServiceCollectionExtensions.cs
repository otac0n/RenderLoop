// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddKeyedSingleton<TService, TKey>(
            this IServiceCollection services,
            TKey serviceKey,
            Func<IServiceProvider, TKey, TService> implementationFactory)
            where TService : class =>
            services.AddKeyedSingleton<TService>(serviceKey, (IServiceProvider s, object? key) => implementationFactory(s, (TKey)key!));
    }
}
