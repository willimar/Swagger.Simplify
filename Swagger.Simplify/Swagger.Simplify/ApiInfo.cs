using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swagger.Simplify.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swagger.Simplify
{
    public class ApiInfo
    {
        public int MajorVersion { get; set; } = 1;
        public int MinorVersion { get; set; } = 1;
        public string GroupNameFormat { get; set; } = $"'v'VVV";
        public bool SubstituteApiVersionInUrl { get; set; } = true;
        public bool AssumeDefaultVersionWhenUnspecified { get; set; } = false;
        public bool ReportApiVersions { get; set; } = true;
        public OpenApiInfo Info { get; set; } = new OpenApiInfo();
        public OpenApiSecurityScheme SecurityScheme { get; set; } = ApiInfo.SecuritySchemeFactory();
        public string KeyNameToXmlComents { get; set; } = string.Empty;
        public Func<ApiDescription, IList<string>> TagActionBy { get; set; } = ApiInfo.GetTagActionBy();

        public static ApiInfo Factory()
        {
            return new ApiInfo();
        }

        public virtual OpenApiSecurityRequirement SecurityRequirementFactory()
        {
            var result = new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme()
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = this.SecurityScheme.Scheme,
                        }
                    },
                    Array.Empty<string>()
                }
            };

            return result;
        }

        private static OpenApiSecurityScheme SecuritySchemeFactory()
        {
            var result = new OpenApiSecurityScheme()
            {
                Description = Resources.SecuritySchemeDescription,
                Name = "Authorization",
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey
            };

            return result;
        }

        private static Func<ApiDescription, IList<string>> GetTagActionBy()
        {
            return (api) =>
            {
                if (api.GroupName != null)
                {
                    return new[] { $"{api.GroupName}" };
                }

                if (api.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
                {
                    return new[] { controllerActionDescriptor.ControllerName };
                }

                throw new InvalidOperationException(Resources.GetTagActionException);
            };
        }
    }
}
