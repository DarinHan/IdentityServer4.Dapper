using System;
using Xunit;
using IdentityServer4.Dapper.DefaultProviders;
using Microsoft.Extensions.DependencyInjection;
using IdentityServer4.Dapper.Options;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Collections.Generic;
using IdentityServer4.Models;
using IdentityServer4;
using IdentityModel;
using IdentityServer4.Dapper.Extensions;

namespace IdentityServer.Dapper.xUnitTest
{
    public class xTestDefaultIdentityResourceProvider
    {
        private DefaultIdentityResourceProvider GetDefaultIdentityResourceProvider()
        {
            DBProviderOptions options = IdentityServerDapperDBExtensions.GetDefaultOptions();
            options.ConnectionString = xTestBase.ConnectionString;

            return new DefaultIdentityResourceProvider(options, null);
        }

        [Fact]
        public void TestAdd()
        {
            var provider = GetDefaultIdentityResourceProvider();
            var openid = new IdentityResources.OpenId()
            {
                Name = "OpenID",
                DisplayName = "OpenID",
                Description = "OpenID"
            };

            var dbidentity = provider.FindIdentityResourcesByName(openid.Name);
            if (dbidentity == null)
            {
                provider.Add(openid);
            }
            dbidentity = provider.FindIdentityResourcesByName(openid.Name);
            Assert.False(dbidentity == null);
        }

        [Fact]
        public void TestAddFindIdentityResourcesByName()
        {
            var provider = GetDefaultIdentityResourceProvider();
            var identity = new IdentityResource("custom.profile", new[] { JwtClaimTypes.Name, JwtClaimTypes.Email, "location" });
            var dbidentity = provider.FindIdentityResourcesByName(identity.Name);
            if (dbidentity == null)
            {
                provider.Add(identity);
            }
            dbidentity = provider.FindIdentityResourcesByName(identity.Name);
            Assert.False(dbidentity == null);
        }

        [Fact]
        public void TestRemove()
        {
            var provider = GetDefaultIdentityResourceProvider();
            var identity = new IdentityResource("TestRemove", new[] { JwtClaimTypes.Name, JwtClaimTypes.Email, "location" });
            var dbidentity = provider.FindIdentityResourcesByName(identity.Name);
            if (dbidentity == null)
            {
                provider.Add(identity);
            }

            provider.Remove(identity.Name);
            dbidentity = provider.FindIdentityResourcesByName(identity.Name);
            Assert.False(dbidentity != null);
            var claims = provider.GetClaimsByName(identity.Name);
            Assert.False(claims != null && claims.Count() > 0);
        }

        [Fact]
        public void TestSearch()
        {
            var provider = GetDefaultIdentityResourceProvider();
            var identity = new IdentityResource("TestSearch", "TestDisplayName", new[] { JwtClaimTypes.Name, JwtClaimTypes.Email, "location" });
            identity.Description = "TestDescription";
            var dbidentity = provider.FindIdentityResourcesByName(identity.Name);
            if (dbidentity == null)
            {
                provider.Add(identity);
            }

            int totalcount = 0;
            var result = provider.Search(identity.Name, 1, 10, out totalcount);
            Assert.False(totalcount <= 0, "total count <= 0");
            Assert.False(result == null || result.Count() == 0, "result is empty");

            result = provider.Search("TestDisplayName", 1, 10, out totalcount);
            Assert.False(totalcount <= 0, "total count <= 0");
            Assert.False(result == null || result.Count() == 0, "result is empty");

            result = provider.Search("TestDescription", 1, 10, out totalcount);
            Assert.False(totalcount <= 0, "total count <= 0");
            Assert.False(result == null || result.Count() == 0, "result is empty");
        }
    }
}
