using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.Dapper.Host.ViewModels
{
    public class VSearchModel
    {
        public string Keywords { get; set; }

        public int PageIndex { get; set; }

        public int PageSize { get; set; }

    }

    public class VSearchModel<T> : VSearchModel where T : class
    {
        public T Extension { get; set; }
    }
}
