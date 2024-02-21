namespace RenderLoop
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Extensions.DependencyInjection;

    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddKeyedSingleton<TService, TKey>(
            this IServiceCollection services,
            TKey serviceKey,
            Func<IServiceProvider, TKey, TService> implementationFactory)
            where TService : class =>
            services.AddKeyedSingleton<TService>(serviceKey, (IServiceProvider s, object? key) => implementationFactory(s, (TKey)key!));

        public static IServiceCollection AddInheritedTypes<TBase>(this IServiceCollection services, string? @namespace, Func<Type, IServiceCollection> register) =>
            services.AddInheritedTypes(typeof(TBase), @namespace, register);

        public static IServiceCollection AddInheritedTypes(this IServiceCollection services, Type baseClass, string? @namespace, Func<Type, IServiceCollection> register)
        {
            var types = from t in Assembly.GetCallingAssembly().GetTypes()
                        where t.Namespace == @namespace
                        where baseClass.IsAssignableFrom(t)
                        select t;

            foreach (var type in types)
            {
                register(type);
            }

            return services;
        }
    }
}
