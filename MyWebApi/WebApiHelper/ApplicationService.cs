using System;
using System.Collections.Generic;
using System.Text;

namespace WebApiHelper
{
    public abstract class ApplicationService : IApplicationService
    {
        public static string[] CommonPostfixes { get; set; } = { "AppService", "ApplicationService" };
    }
}
