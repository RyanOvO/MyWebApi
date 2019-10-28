using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace WebApiHelper
{
    public class MyWebApiConvention : IApplicationModelConvention
    {
        private readonly IServiceCollection _services;
        public MyWebApiConvention(IServiceCollection services) => _services = services; 

        public void Apply(ApplicationModel application)
        {
            if(application.Controllers.Count <= 0)
            {
                return;
            }

            foreach(var controller in application.Controllers)
            {
                var controllerType = controller.ControllerType.AsType();

                if(typeof(IRemoteService).GetTypeInfo().IsAssignableFrom(controllerType))
                {
                    controller.ControllerName = controller.ControllerName.RemovePostFix(ApplicationService.CommonPostfixes);
                    ConfigureDynamicWebApi(controller);
                }
            }
        }

        private void ConfigureDynamicWebApi(ControllerModel controller)
        {
            ConfigureApiExplorer(controller);
            // Api选择器，主要是路由配置
            ConfigureSelector(controller);
            ConfigureParameters(controller);
        }

        private void ConfigureParameters(ControllerModel controller)
        {
            foreach (var action in controller.Actions)
            {
                foreach (var para in action.Parameters)
                {
                    if (para.BindingInfo != null)
                    {
                        continue;
                    }

                    if (!TypeHelper.IsPrimitiveExtendedIncludingNullable(para.ParameterInfo.ParameterType))
                    {
                        if (CanUseFormBodyBinding(action, para))
                        {
                            para.BindingInfo = BindingInfo.GetBindingInfo(new[] { new FromBodyAttribute() });
                        }
                    }
                }
            }
        }

        #region ApiExplorer
        private void ConfigureApiExplorer(ControllerModel controller)
        {
            if (controller.ApiExplorer.GroupName.IsNullOrEmpty())
            {
                controller.ApiExplorer.GroupName = controller.ControllerName;
            }

            if (controller.ApiExplorer.IsVisible == null)
            {
                controller.ApiExplorer.IsVisible = true;
            }

            foreach (var action in controller.Actions)
            {
                ConfigureApiExplorer(action);
            }
        }

        private void ConfigureApiExplorer(ActionModel action)
        {
            if (action.ApiExplorer.IsVisible == null)
            {
                action.ApiExplorer.IsVisible = true;
            }
        }

        #endregion

        private void ConfigureSelector(ControllerModel controller)
        {
            RemoveEmptySelectors(controller.Selectors);

            if (controller.Selectors.Any(selector => selector.AttributeRouteModel != null))
            {
                return;
            }

            var areaName = string.Empty;
            foreach (var action in controller.Actions)
            {
                ConfigureSelector(areaName, controller.ControllerName, action);
            }
        }

        private void ConfigureSelector(string areaName, string controllerName, ActionModel action)
        {
            RemoveEmptySelectors(action.Selectors);
            if (!action.Selectors.Any())
            {
                AddAppServiceSelector(areaName, controllerName, action);
            }
            else
            {
                NormalizeSelectorRoutes(areaName, controllerName, action);
            }
        }

        private void AddAppServiceSelector(string areaName, string controllerName, ActionModel action)
        {
            string verb;
            var verbKey = action.ActionName.GetPascalOrCamelCaseFirstWord().ToLower();
            verb = AppConsts.HttpVerbs.ContainsKey(verbKey) ? AppConsts.HttpVerbs[verbKey] : AppConsts.DefaultHttpVerb;

            action.ActionName = GetRestFulActionName(action.ActionName);
            var appServiceSelectorModel = new SelectorModel
            {
                AttributeRouteModel = CreateActionRouteModel(areaName, controllerName, action.ActionName)
            };

            appServiceSelectorModel.ActionConstraints.Add(new HttpMethodActionConstraint(new[] { verb }));

            action.Selectors.Add(appServiceSelectorModel);
        }

        private bool CanUseFormBodyBinding(ActionModel action, ParameterModel parameter)
        {
            if (AppConsts.FormBodyBindingIgnoredTypes.Any(t => t.IsAssignableFrom(parameter.ParameterInfo.ParameterType)))
            {
                return false;
            }

            foreach (var selector in action.Selectors)
            {
                if (selector.ActionConstraints == null)
                {
                    continue;
                }

                foreach (var actionConstraint in selector.ActionConstraints)
                {
                    var httpMethodActionConstraint = actionConstraint as HttpMethodActionConstraint;
                    if (httpMethodActionConstraint == null)
                    {
                        continue;
                    }

                    if (httpMethodActionConstraint.HttpMethods.All(hm => hm.IsIn("GET", "DELETE", "TRACE", "HEAD")))
                    {
                        return false;
                    }
                }
            }

            return true;
        }


        /// <summary>
        /// Processing action name
        /// </summary>
        /// <param name="actionName"></param>
        /// <returns></returns>
        private static string GetRestFulActionName(string actionName)
        {
            // Remove Postfix
            actionName = actionName.RemovePostFix(AppConsts.ActionPostfixes.ToArray());

            // Remove Prefix
            var verbKey = actionName.GetPascalOrCamelCaseFirstWord().ToLower();
            if (AppConsts.HttpVerbs.ContainsKey(verbKey))
            {
                if (actionName.Length == verbKey.Length)
                {
                    return "";
                }
                else
                {
                    return actionName.Substring(verbKey.Length);
                }
            }
            else
            {
                return actionName;
            }
        }

        private static void NormalizeSelectorRoutes(string areaName, string controllerName, ActionModel action)
        {
            action.ActionName = GetRestFulActionName(action.ActionName);
            foreach (var selector in action.Selectors)
            {
                selector.AttributeRouteModel = selector.AttributeRouteModel == null ?
                    CreateActionRouteModel(areaName, controllerName, action.ActionName) :
                    AttributeRouteModel.CombineAttributeRouteModel(CreateActionRouteModel(areaName, controllerName, ""), selector.AttributeRouteModel);
            }
        }
        
        private static AttributeRouteModel CreateActionRouteModel(string areaName, string controllerName, string actionName)
        {
            var routeStr =
                $"{AppConsts.DefaultApiPreFix}/{areaName}/{controllerName}/{actionName}".Replace("//", "/");
            return new AttributeRouteModel(new RouteAttribute(routeStr));
        }


        private static void RemoveEmptySelectors(IList<SelectorModel> selectors)
        {
            selectors
                .Where(IsEmptySelector)
                .ToList()
                .ForEach(s => selectors.Remove(s));
        }

        private static bool IsEmptySelector(SelectorModel selector)
        {
            return selector.AttributeRouteModel == null && selector.ActionConstraints.IsNullOrEmpty();
        }

    }
}
