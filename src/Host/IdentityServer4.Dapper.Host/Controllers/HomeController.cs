using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Dapper.Host.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OneSmart.Core;

namespace IdentityServer4.Dapper.Host.Controllers
{
    [Produces("application/json")]
    public class HomeController : Controller
    {
        [HttpGet("MenuList")]
        public ApiResult<IEnumerable<MenuInfo>> GetMenuList(string userid)
        {
            return null;
        }
    }
}