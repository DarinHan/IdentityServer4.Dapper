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
        
        private DefaultApiResourceProvider GetDefaultApiResourceProvider(string sqltype)
        {
            return new DefaultApiResourceProvider(xTestBase.GetDBProviderOptions(sqltype), null);
        }

        [Theory]
        [InlineData(xTestBase.MSSQL)]
        [InlineData(xTestBase.MySQL)]
        public void TestAddFind(string sqltype)
        {
            var provider = GetDefaultApiResourceProvider(sqltype);
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

        [Theory]
        [InlineData(xTestBase.MSSQL)]
        [InlineData(xTestBase.MySQL)]
        public void TestRemove(string sqltype)
        {
            var provider = GetDefaultApiResourceProvider(sqltype);
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

        [Theory]
        [InlineData(xTestBase.MSSQL)]
        [InlineData(xTestBase.MySQL)]
        public void TestFindAll(string sqltype)
        {
            var provider = GetDefaultApiResourceProvider(sqltype);
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

        [Theory]
        [InlineData(xTestBase.MSSQL)]
        [InlineData(xTestBase.MySQL)]
        public void TestFindApiResourcesByScope(string sqltype)
        {
            var provider = GetDefaultApiResourceProvider(sqltype);
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

        [Theory]
        [InlineData(xTestBase.MSSQL)]
        [InlineData(xTestBase.MySQL)]
        public void TestSearch(string sqltype)
        {
            var provider = GetDefaultApiResourceProvider(sqltype);
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
