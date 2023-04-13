using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ApiDescriptions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection {

    public static class SwaggerGenServiceCollectionExtensions {
        // Alterado nome da extensão AddSwaggerGen -> AddSwaggerGen_Mod
        public static IServiceCollection AddSwaggerGen_Mod(
            this IServiceCollection services,
            Action<SwaggerGenOptions> setupAction = null) {
            // Add Mvc convention to ensure ApiExplorer is enabled for all actions
            services.Configure<MvcOptions>(c =>
                c.Conventions.Add(new SwaggerApplicationConvention()));

            // Register custom configurators that takes values from SwaggerGenOptions (i.e. high level config)
            // and applies them to SwaggerGeneratorOptions and SchemaGeneratorOptoins (i.e. lower-level config)
            services.AddTransient<IConfigureOptions<SwaggerGeneratorOptions>, ConfigureSwaggerGeneratorOptions>();
            services.AddTransient<IConfigureOptions<SchemaGeneratorOptions>, ConfigureSchemaGeneratorOptions>();

            // Register generator and it's dependencies
            // Alterado SwaggerGenerator -> SwaggerGenerator_Mod
            services.TryAddTransient<ISwaggerProvider, SwaggerGenerator_Mod>();
            // Alterado SwaggerGenerator -> SwaggerGenerator_Mod
            services.TryAddTransient<IAsyncSwaggerProvider, SwaggerGenerator_Mod>();
            services.TryAddTransient(s => s.GetRequiredService<IOptions<SwaggerGeneratorOptions>>().Value);
            services.TryAddTransient<ISchemaGenerator, SchemaGenerator>();
            services.TryAddTransient(s => s.GetRequiredService<IOptions<SchemaGeneratorOptions>>().Value);
            services.TryAddTransient<ISerializerDataContractResolver>(s => {
#if (!NETSTANDARD2_0)
                var serializerOptions = s.GetService<IOptions<JsonOptions>>()?.Value?.JsonSerializerOptions
                    ?? new JsonSerializerOptions();
#else
            var serializerOptions = new JsonSerializerOptions();
#endif

                return new JsonSerializerDataContractResolver(serializerOptions);
            });

            // Used by the <c>dotnet-getdocument</c> tool from the Microsoft.Extensions.ApiDescription.Server package.
            services.TryAddSingleton<IDocumentProvider, DocumentProvider>();

            if (setupAction != null) services.ConfigureSwaggerGen_Mod(setupAction);

            return services;
        }

        // Alterado SwaggerGenerator -> SwaggerGenerator_Mod
        public static void ConfigureSwaggerGen_Mod(
            this IServiceCollection services,
            Action<SwaggerGenOptions> setupAction) {
            services.Configure(setupAction);
        }
    }
}