using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.Dapper.Host.Models
{
    public class MenuInfo
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public int Index { get; set; }

        public bool Enabled { get; set; }

        public IEnumerable<MenuInfo> SubMenus { get; set; }
    }
}
