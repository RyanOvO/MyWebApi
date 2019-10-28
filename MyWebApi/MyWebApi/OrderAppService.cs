using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApiHelper;

namespace MyWebApi
{
    public class OrderAppService : ApplicationService
    {
        private static readonly Dictionary<int, string> Apples = new Dictionary<int, string>()
        {
            [1] = "Big Apple",
            [2] = "Small Apple"
        };

        /// <summary>
        /// Get All Apple.
        /// </summary>
        /// <returns></returns>
        public string GetAll(int id)
        {
            return "111";
        }

        /// <summary>
        /// action
        /// 路由  /api/Order/OrderById?Id=6
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetOrderById(int id)
        {
            return $"自定义Api控制器方法返回的参数{id}";
        }
    }
}
