using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebApiHelper
{
    public static class MyWebApiServiceExtension
    {
        /// <summary>
        /// Add Dynamic WebApi to Container
        /// </summary>
        /// <param name="services"></param>
        /// <param name="options">configuration</param>
        /// <returns></returns>
        public static IServiceCollection AddMyWebApi(this IServiceCollection services, MyWebApiOptions options)
        {
            if (options == null)
            {
                throw new ArgumentException(nameof(options));
            }

            options.Valid();

            AppConsts.DefaultAreaName = options.DefaultAreaName;
            AppConsts.DefaultHttpVerb = options.DefaultHttpVerb;
            AppConsts.DefaultApiPreFix = options.DefaultApiPrefix;
            AppConsts.ControllerPostfixes = options.RemoveControllerPostfixes;
            AppConsts.ActionPostfixes = options.RemoveActionPostfixes;
            AppConsts.FormBodyBindingIgnoredTypes = options.FormBodyBindingIgnoredTypes;

            var partManager = services.FirstOrDefault(f => f.ServiceType == typeof(ApplicationPartManager))?.ImplementationInstance as ApplicationPartManager;

            if (partManager == null)
            {
                throw new InvalidOperationException("\"AddDynamicWebApi\" must be after \"AddMvc\".");
            }

            // 自定义控制器入口
            partManager.FeatureProviders.Add(new MyWebApiControllerFeatureProvider());

            // 以约定的形式覆盖控制器
            services.Configure<MvcOptions>(o =>
            {
                // Register Controller Routing Information Converter
                o.Conventions.Add(new MyWebApiConvention(services));
            });

            return services;
        }

        public static IServiceCollection AddMyWebApi(this IServiceCollection services)
        {
            return AddMyWebApi(services, new MyWebApiOptions());
        }
    }
}
