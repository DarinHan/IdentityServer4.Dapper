using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OneSmart.Core;
using IdentityServer4.Models;
using IdentityServer4.Dapper.Interfaces;
using IdentityServer4.Dapper.Host.ViewModels;

namespace IdentityServer4.Dapper.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class ClientsController : ControllerBase
    {
        [HttpPost]
        public ApiResult<IEnumerable<Client>> IndexList([FromServices] IClientProvider clientProvider, VSearchModel vSearchModel)
        {
            var lst = clientProvider.Search(vSearchModel.Keywords, vSearchModel.PageIndex, vSearchModel.PageSize, out int count);
            return new ApiResult<IEnumerable<Client>>()
            {
                Data = lst
            };
        }
    }
}