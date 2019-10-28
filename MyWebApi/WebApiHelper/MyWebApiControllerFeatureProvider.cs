using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace WebApiHelper
{
    public class MyWebApiControllerFeatureProvider : ControllerFeatureProvider
    {
        protected override bool IsController(TypeInfo typeInfo)
        {
            var type = typeInfo.AsType();

            if (!typeof(IRemoteService).IsAssignableFrom(type) ||
                !typeInfo.IsPublic || typeInfo.IsAbstract || typeInfo.IsGenericType)
            {
                return false;
            }

            return true;
        }
    }
}
