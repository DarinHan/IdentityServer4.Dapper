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
using IdentityServer4.Dapper.Extensions;

namespace IdentityServer.Dapper.xUnitTest
{
    public class xTestDefaultClientProvider
    {
        private DefaultClientProvider GetDefaultClientProvider(string sqltype)
        {
            return new DefaultClientProvider(xTestBase.GetDBProviderOptions(sqltype), null);
        }

        [Theory]
        [InlineData(xTestBase.MSSQL)]
        [InlineData(xTestBase.MySQL)]
        public void TestAdd(string sqltype)
        {
            var provider = GetDefaultClientProvider(sqltype);
            string name = $"client{DateTime.Now.ToString("yyyyMMddhhmmss")}";
            provider.Add(new Client
            {
                ClientId = name,
                ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                AllowedScopes = { "api1", "api2.read_only" },
                Properties =
                    {
                        { "foo", "bar" }
                    }
            });

            var client = provider.FindClientById(name);
            Assert.False(client == null, "add failed");
        }

        [Theory]
        [InlineData(xTestBase.MSSQL)]
        [InlineData(xTestBase.MySQL)]
        public void TestRemove(string sqltype)
        {
            var provider = GetDefaultClientProvider(sqltype);
            var dbitem = provider.GetById("ClientId");
            if (dbitem == null)
            {
                provider.Add(new Client
                {
                    ClientId = "ClientId",
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes = { "api1", "api2.read_only" },
                    Properties =
                    {
                        { "foo", "bar" }
                    }
                });
            }
            var entity = provider.GetById("ClientId");
            provider.Remove("ClientId");

            dbitem = provider.GetById("ClientId");
            Assert.False(dbitem != null, "add failed");

            var clientGrantTypes = provider.GetClientGrantTypeByClientID(entity.Id);
            Assert.False(clientGrantTypes != null && clientGrantTypes.Count() > 0, "remove failed");
            var redirectUris = provider.GetClientRedirectUriByClientID(entity.Id);
            Assert.False(redirectUris != null && redirectUris.Count() > 0, "remove failed");
            var postLogoutRedirectUris = provider.GetClientPostLogoutRedirectUriByClientID(entity.Id);
            Assert.False(postLogoutRedirectUris != null && postLogoutRedirectUris.Count() > 0, "remove failed");
            var scopes = provider.GetClientScopeByClientID(entity.Id);
            Assert.False(scopes != null && scopes.Count() > 0, "remove failed");
            var secrets = provider.GetClientSecretByClientID(entity.Id);
            Assert.False(secrets != null && secrets.Count() > 0, "remove failed");
            var clientClaims = provider.GetClientClaimByClientID(entity.Id);
            Assert.False(clientClaims != null && clientClaims.Count() > 0, "remove failed");
            var restrictions = provider.GetClientIdPRestrictionByClientID(entity.Id);
            Assert.False(restrictions != null && restrictions.Count() > 0, "remove failed");
            var corsOrigins = provider.GetClientCorsOriginByClientID(entity.Id);
            Assert.False(corsOrigins != null && corsOrigins.Count() > 0, "remove failed");
            var properties = provider.GetClientPropertyByClientID(entity.Id);
            Assert.False(properties != null && properties.Count() > 0, "remove failed");

        }

        [Theory]
        [InlineData(xTestBase.MSSQL)]
        [InlineData(xTestBase.MySQL)]
        public void TestQueryAllowedCorsOrigins(string sqltype)
        {
            var provider = GetDefaultClientProvider(sqltype);
            var entity = provider.GetById("js_oidc");
            if (entity == null)
            {
                provider.Add(new Client
                {
                    ClientId = "js_oidc",
                    ClientName = "JavaScript OIDC Client",
                    ClientUri = "http://identityserver.io",
                    //LogoUri = "https://pbs.twimg.com/profile_images/1612989113/Ki-hanja_400x400.png",

                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,
                    RequireClientSecret = false,
                    AccessTokenType = AccessTokenType.Jwt,

                    RedirectUris =
                    {
                        "http://localhost:7017/index.html",
                        "http://localhost:7017/callback.html",
                        "http://localhost:7017/silent.html",
                        "http://localhost:7017/popup.html"
                    },

                    PostLogoutRedirectUris = { "http://localhost:7017/index.html" },
                    AllowedCorsOrigins = { "http://localhost:7017" },

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "api1", "api2.read_only", "api2.full_access"
                    }
                });
            }
            var origins = provider.QueryAllowedCorsOrigins();
            Assert.False(origins == null || origins.Count() == 0);
            var def = origins.FirstOrDefault(c => c == "http://localhost:7017");
            Assert.False(string.IsNullOrEmpty(def));

        }

        [Theory]
        [InlineData(xTestBase.MSSQL)]
        [InlineData(xTestBase.MySQL)]
        public void TestSearch(string sqltype)
        {
            var provider = GetDefaultClientProvider(sqltype);
            var dbitem = provider.GetById("js_oauth");
            if (dbitem == null)
            {
                provider.Add(new Client
                {
                    ClientId = "js_oauth",
                    ClientName = "JavaScript OAuth 2.0 Client",
                    ClientUri = "http://identityserver.io",
                    //LogoUri = "https://pbs.twimg.com/profile_images/1612989113/Ki-hanja_400x400.png",

                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,

                    RedirectUris = { "http://localhost:28895/index.html" },
                    AllowedScopes = { "api1", "api2.read_only" },
                });
            }
            int totalcount = 0;
            var result = provider.Search("js_oauth", 1, 10, out totalcount);
            Assert.False(totalcount <= 0, "total count <= 0");
            Assert.False(result == null || result.Count() == 0, "result is empty");
            result = provider.Search("JavaScript OAuth 2.0 Client", 1, 10, out totalcount);
            Assert.False(totalcount <= 0, "total count <= 0");
            Assert.False(result == null || result.Count() == 0, "result is empty");
        }
    }
}
