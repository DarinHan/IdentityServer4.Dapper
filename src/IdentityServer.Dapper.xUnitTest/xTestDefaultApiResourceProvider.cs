using System;
using Xunit;
using IdentityServer4.Dapper.DefaultProviders;
using Microsoft.Extensions.DependencyInjection;
using IdentityServer4.Dapper.Options;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Collections.Generic;
using IdentityServer4.Models;
using IdentityModel;
using IdentityServer4.Dapper.Extensions;

namespace IdentityServer.Dapper.xUnitTest
{
    public class xTestDefaultApiResourceProvider
    {

        private DefaultApiResourceProvider GetDefaultApiResourceProvider()
        {
            DBProviderOptions options = IdentityServerDapperDBExtensions.GetDefaultOptions();
            options.ConnectionString = xTestBase.ConnectionString;

            return new DefaultApiResourceProvider(options, null);
        }

        private void ClearApiResource()
        {
            var provider = GetDefaultApiResourceProvider();
            var lst = provider.FindApiResourcesAll();
            if (lst != null)
            {
                foreach (var item in lst)
                {
                    provider.Remove(item.Name);
                }
            }
        }

        [Fact]
        public void TestAddFind()
        {
            var provider = GetDefaultApiResourceProvider();
            string name = $"TestName";
            var dbitem = provider.FindApiResource(name);
            if (dbitem != null)
            {
                provider.Remove(name);
            }
            provider.Add(new IdentityServer4.Models.ApiResource()
            {
                Name = name,
                DisplayName = "TestDisplayApiResourceName",
                Enabled = true,
                Description = "TestApiResourceDescription"
            });

            var apiresource = provider.FindApiResource(name);
            Assert.False(apiresource == null, "add failed");
        }

        [Fact]
        public void TestRemove()
        {
            var provider = GetDefaultApiResourceProvider();
            var apiresouece = provider.FindApiResource("api2");
            if (apiresouece == null)
            {
                provider.Add(new ApiResource
                {
                    Name = "api2",

                    ApiSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    UserClaims =
                    {
                        JwtClaimTypes.Name,
                        JwtClaimTypes.Email
                    },

                    Scopes =
                    {
                        new Scope()
                        {
                            Name = "api2.full_access",
                            DisplayName = "Full access to API 2"
                        },
                        new Scope
                        {
                            Name = "api2.read_only",
                            DisplayName = "Read only access to API 2"
                        }
                    }
                });
            }
            var entity = provider.GetByName("api2");
            provider.Remove("api2");
            apiresouece = provider.FindApiResource("api2");
            Assert.False(apiresouece != null, "remove failed");

            var sec = provider.GetSecretByAPIID(entity.Id);
            Assert.False(sec != null && sec.Count() > 0, "remove failed");
            var claims = provider.GetClaimsByAPIID(entity.Id);
            Assert.False(claims != null && claims.Count() > 0, "remove failed");
            var scopes = provider.GetScopesByAPIID(entity.Id);
            Assert.False(scopes != null && scopes.Count() > 0, "remove failed");
        }

        [Fact]
        public void TestFindAll()
        {
            var provider = GetDefaultApiResourceProvider();
            var item = provider.FindApiResource("TestDisplayApiResourceName");
            if (item == null)
            {
                provider.Add(new IdentityServer4.Models.ApiResource()
                {
                    Name = $"TestDisplayApiResourceName",
                    DisplayName = "TestDisplayApiResourceName",
                    Enabled = true,
                    Description = "TestApiResourceDescription"
                });
            }

            var lstall = provider.FindApiResourcesAll();
            Assert.False(lstall == null || lstall.Count() == 0, "FindApiResourcesAll failed");
        }

        [Fact]
        public void TestFindApiResourcesByScope()
        {
            var provider = GetDefaultApiResourceProvider();
            var api = provider.FindApiResource("TestDisplayApiResourceNameScope");
            if (api == null)
            {
                provider.Add(new IdentityServer4.Models.ApiResource()
                {
                    Name = $"TestDisplayApiResourceNameScope",
                    DisplayName = "TestDisplayApiResourceName",
                    Enabled = true,
                    Description = "TestApiResourceDescription",
                    Scopes = new List<IdentityServer4.Models.Scope>()
                {
                    new IdentityServer4.Models.Scope()
                    {
                        Name = "TestScope"
                    }
                }
                });
            }

            var apis = provider.FindApiResourcesByScope(new List<string>() { "TestScope" });
            Assert.False(apis == null, "TestScopeFailed");
            var defaultapi = apis.FirstOrDefault(c => c.Name == "TestDisplayApiResourceNameScope");
            Assert.False(defaultapi == null, "TestScopeFailed");
        }

        [Fact]
        public void TestSearch()
        {
            var provider = GetDefaultApiResourceProvider();
            string name = "TestSearch";
            var item = provider.FindApiResource(name);
            if (item == null)
            {
                provider.Add(new ApiResource
                {
                    Name = name,

                    ApiSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    UserClaims =
                    {
                        JwtClaimTypes.Name,
                        JwtClaimTypes.Email
                    },

                    Scopes =
                    {
                        new Scope()
                        {
                            Name = "api2.full_access",
                            DisplayName = "Full access to API 2"
                        },
                        new Scope
                        {
                            Name = "api2.read_only",
                            DisplayName = "Read only access to API 2"
                        }
                    },
                    Description = "TestDescriptionSearch",
                    DisplayName = "TestDisplayNameSearch",
                });
            }
            int totalcount;
            var result = provider.Search(name, 1, 10, out totalcount);
            Assert.False(totalcount <= 0, "total count <= 0");
            Assert.False(result == null || result.Count() == 0, "result is empty");
            result = provider.Search("TestDescriptionSearch", 1, 10, out totalcount);
            Assert.False(totalcount <= 0, "total count <= 0");
            Assert.False(result == null || result.Count() == 0, "result is empty");
            result = provider.Search("TestDisplayNameSearch", 1, 10, out totalcount);
            Assert.False(totalcount <= 0, "total count <= 0");
            Assert.False(result == null || result.Count() == 0, "result is empty");
        }
    }
}
