using Microsoft.AspNetCore.Mvc;
using Swagger.Simplify;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Builder;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Reflection;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Linq;
using System.Threading;
using System.IO;
using Swagger.Simplify.Properties;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SwaggerSetup
    {
        public const string AllowAnyOrigins = "_AllowAnyOrigins";
        public const string OptionsPolicy = "_OptionsPolicy";

        public static void GenerateToVersion(this IServiceCollection services, ApiInfo apiInfo)
        {
            if (apiInfo is null)
            {
                throw new ArgumentNullException(nameof(apiInfo));
            }

            services.ConfigureVersions(apiInfo);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc($"v{apiInfo.MajorVersion}", apiInfo.Info);
                c.AddSecurityDefinition(apiInfo.SecurityScheme.Scheme, apiInfo.SecurityScheme);
                c.AddSecurityRequirement(apiInfo.SecurityRequirementFactory());
                c.IncludeAllXmlComents(apiInfo.KeyNameToXmlComents);
                c.TagActionsBy(apiInfo.TagActionBy);
                c.DocInclusionPredicate((name, api) => true);
                c.EnableAnnotations();
            });
            
        }

        public static void PrepareAnyCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(SwaggerSetup.AllowAnyOrigins,
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials();
                    });

                options.AddPolicy(SwaggerSetup.OptionsPolicy, builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();
                });
            });
        }

        public static void ConfigureInSwaggerSimplify(this IApplicationBuilder app, CultureInfo cultureInfo, CultureInfo[] supportedCultures, Assembly assembly)
        {
            if (cultureInfo is null)
            {
                throw new ArgumentNullException(nameof(cultureInfo));
            }

            if (supportedCultures is null || !supportedCultures.Any())
            {
                supportedCultures = new CultureInfo[] { cultureInfo };
            }

            Thread.CurrentThread.CurrentCulture = cultureInfo;

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(cultureInfo.Name),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures,
            });

            app.UseSwagger();

            var provider = app.ApplicationServices.GetService<IApiVersionDescriptionProvider>() ?? throw new ArgumentNullException(Resources.apiVersionDescriptionProviderNotFound);
            (AssemblyDescriptionAttribute descriptionAttribute, AssemblyProductAttribute productAttribute, AssemblyCopyrightAttribute copyrightAttribute, AssemblyName assemblyName) = SwaggerSetup.GetAssemblyInfo(assembly);

            app.UseSwaggerUI(options =>
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    $"{productAttribute?.Product} - {description.GroupName}");
                }

                options.DocExpansion(DocExpansion.List);
            });

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCors(SwaggerSetup.AllowAnyOrigins);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private static (AssemblyDescriptionAttribute descriptionAttribute, AssemblyProductAttribute productAttribute, AssemblyCopyrightAttribute copyrightAttribute, AssemblyName assemblyName) GetAssemblyInfo(Assembly assembly)
        {
            var assemblyInfo = assembly.GetName();

            var descriptionAttribute = assembly
                 .GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)
                 .OfType<AssemblyDescriptionAttribute>()
                 .FirstOrDefault();
            var productAttribute = assembly
                 .GetCustomAttributes(typeof(AssemblyProductAttribute), false)
                 .OfType<AssemblyProductAttribute>()
                 .FirstOrDefault();
            var copyrightAttribute = assembly
                 .GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)
                 .OfType<AssemblyCopyrightAttribute>()
                 .FirstOrDefault();

            return (descriptionAttribute, productAttribute, copyrightAttribute, assemblyInfo);
        }

        private static void IncludeAllXmlComents(this SwaggerGenOptions swaggerGenOptions, string keyName)
        {
            var pathToXmlDocumentsToLoad = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(x => x.FullName != null && x.FullName.ToLower().Contains(keyName.ToLower()))
                    .Select(x => Path.Combine(AppContext.BaseDirectory, $"{x.GetName().Name}.xml"))
                    .Where(x => File.Exists(x))
                    .ToList();

            pathToXmlDocumentsToLoad.ForEach(doc => swaggerGenOptions.IncludeXmlComments(doc));
        }

        private static void ConfigureVersions(this IServiceCollection services, ApiInfo apiInfo)
        {
            services.AddApiVersioning(x =>
            {
                x.DefaultApiVersion = new ApiVersion(apiInfo.MajorVersion, apiInfo.MinorVersion);
                x.AssumeDefaultVersionWhenUnspecified = apiInfo.AssumeDefaultVersionWhenUnspecified;
                x.ReportApiVersions = apiInfo.ReportApiVersions;
            });

            services.AddVersionedApiExplorer(p =>
            {
                p.GroupNameFormat = apiInfo.GroupNameFormat;
                p.SubstituteApiVersionInUrl = apiInfo.SubstituteApiVersionInUrl;
            });
        }
    }
}
