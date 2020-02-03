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
using System.Security.Claims;

namespace IdentityServer.Dapper.xUnitTest
{
    public class xTestDefaultClientProvider
    {
        private DefaultClientProvider GetDefaultClientProvider(string sqltype)
        {
            return new DefaultClientProvider(xTestBase.GetDBProviderOptions(sqltype), null, xTestBase.GetCache());
        }

        [Theory]
        [InlineData(xTestBase.MSSQL)]
        [InlineData(xTestBase.MySQL)]
        public void TestAdd(string sqltype)
        {
            var provider = GetDefaultClientProvider(sqltype);
            string name = $"client{DateTime.Now.ToString("yyyyMMddhhmmss")}";

            provider.Remove(name);

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
            var dbitem = provider.FindClientById("ClientId");
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
            var entity = provider.FindClientById("ClientId");
            var entityid = provider.GetClientEntityID("ClientId");
            provider.Remove("ClientId");

            dbitem = provider.FindClientById("ClientId");
            Assert.False(dbitem != null, "remove failed");

            var clientGrantTypes = provider.GetClientGrantTypeByClientID(entityid);
            Assert.False(clientGrantTypes != null && clientGrantTypes.Count() > 0, "remove failed");
            var redirectUris = provider.GetClientRedirectUriByClientID(entityid);
            Assert.False(redirectUris != null && redirectUris.Count() > 0, "remove failed");
            var postLogoutRedirectUris = provider.GetClientPostLogoutRedirectUriByClientID(entityid);
            Assert.False(postLogoutRedirectUris != null && postLogoutRedirectUris.Count() > 0, "remove failed");
            var scopes = provider.GetClientScopeByClientID(entityid);
            Assert.False(scopes != null && scopes.Count() > 0, "remove failed");
            var secrets = provider.GetClientSecretByClientID(entityid);
            Assert.False(secrets != null && secrets.Count() > 0, "remove failed");
            var clientClaims = provider.GetClientClaimByClientID(entityid);
            Assert.False(clientClaims != null && clientClaims.Count() > 0, "remove failed");
            var restrictions = provider.GetClientIdPRestrictionByClientID(entityid);
            Assert.False(restrictions != null && restrictions.Count() > 0, "remove failed");
            var corsOrigins = provider.GetClientCorsOriginByClientID(entityid);
            Assert.False(corsOrigins != null && corsOrigins.Count() > 0, "remove failed");
            var properties = provider.GetClientPropertyByClientID(entityid);
            Assert.False(properties != null && properties.Count() > 0, "remove failed");

        }

        [Theory]
        [InlineData(xTestBase.MSSQL)]
        [InlineData(xTestBase.MySQL)]
        public void TestUpdate(string sqltype)
        {
            var provider = GetDefaultClientProvider(sqltype);
            provider.Remove("TestUpdate");

            var dbitem = provider.FindClientById("TestUpdate");
            if (dbitem != null)
            {
                provider.Remove(dbitem.ClientId);
            }

            provider.Add(new Client
            {
                ClientId = "TestUpdate",
                ClientName = "MVC Hybrid",
                ClientUri = "http://identityserver.io",
                //LogoUri = "https://pbs.twimg.com/profile_images/1612989113/Ki-hanja_400x400.png",

                ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                AllowedGrantTypes = GrantTypes.Hybrid,
                AllowAccessTokensViaBrowser = false,

                RedirectUris = { "http://localhost:21402/signin-oidc" },
                FrontChannelLogoutUri = "http://localhost:21402/signout-oidc",
                PostLogoutRedirectUris = { "http://localhost:21402/signout-callback-oidc" },

                AllowOfflineAccess = true,

                AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "api1", "api2.read_only"
                    }
            });
            var client = provider.FindClientById("TestUpdate");
            client.Description = "Modified Description";
            provider.Update(client);
            client = provider.FindClientById("TestUpdate");
            Assert.False(client.Description != "Modified Description", "update Description failed");

            //UpdateClientGrantTypeByClientID
            client.AllowedGrantTypes = GrantTypes.ClientCredentials;
            provider.Update(client);
            client = provider.FindClientById("TestUpdate");
            Assert.False(!client.AllowedGrantTypes.Contains(GrantType.ClientCredentials), "update UpdateClientGrantTypeByClientID failed");

            //UpdateClientRedirectUriByClientID
            client.RedirectUris = new List<string>() { "http://localhost:21402/signin-oidc-2" };
            provider.Update(client);
            client = provider.FindClientById("TestUpdate");
            Assert.False(!client.RedirectUris.Contains("http://localhost:21402/signin-oidc-2"), "update UpdateClientRedirectUriByClientID failed");

            //UpdateClientPostLogoutRedirectUriByClientID
            client.PostLogoutRedirectUris = new List<string>() { "http://localhost:21402/signout-callback-oidc-2" };
            provider.Update(client);
            client = provider.FindClientById("TestUpdate");
            Assert.False(!client.PostLogoutRedirectUris.Contains("http://localhost:21402/signout-callback-oidc-2"), "update UpdateClientPostLogoutRedirectUriByClientID failed");

            //UpdateClientScopeByClientID
            client.AllowedScopes = new List<string>()
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                    };
            provider.Update(client);
            client = provider.FindClientById("TestUpdate");
            Assert.False(!client.AllowedScopes.Contains(IdentityServerConstants.StandardScopes.OpenId), "update UpdateClientScopeByClientID failed");
            Assert.False(client.AllowedScopes.Contains(IdentityServerConstants.StandardScopes.Email), "update UpdateClientScopeByClientID failed");

            //UpdateClientSecretByClientID
            client.ClientSecrets = new List<Secret>()
                    {
                        new Secret("secret2".Sha256())
                    };
            provider.Update(client);
            client = provider.FindClientById("TestUpdate");
            Assert.False(client.ClientSecrets.FirstOrDefault(c => c.Value == "secret1".Sha256()) != null, "update UpdateClientSecretByClientID failed");
            Assert.False(client.ClientSecrets.FirstOrDefault(c => c.Value == "secret2".Sha256()) == null, "update UpdateClientSecretByClientID failed");

            //UpdateClientClaimByClientID
            client.Claims = new List<Claim>()
                    {
                        new Claim("testType","testvalue")
                    };
            provider.Update(client);
            client = provider.FindClientById("TestUpdate");
            Assert.False(client.Claims.FirstOrDefault(c => c.Type == "testType" && c.Value == "testvalue") == null, "update UpdateClientClaimByClientID failed");


            //UpdateClientIdPRestrictionByClientID
            client.IdentityProviderRestrictions = new List<string>()
                    {
                        "TestIdentityProviderRestrictions"
                    };
            provider.Update(client);
            client = provider.FindClientById("TestUpdate");
            Assert.False(!client.IdentityProviderRestrictions.Contains("TestIdentityProviderRestrictions"), "update UpdateClientIdPRestrictionByClientID failed");

            //UpdateClientCorsOriginByClientID
            client.AllowedCorsOrigins = new List<string>()
                    {
                        "AllowedCorsOrigins"
                    };
            provider.Update(client);
            client = provider.FindClientById("TestUpdate");
            Assert.False(!client.AllowedCorsOrigins.Contains("AllowedCorsOrigins"), "update UpdateClientCorsOriginByClientID failed");

            //UpdateClientPropertyByClientID
            client.Properties = new Dictionary<string, string>();
            client.Properties.Add("TestKey", "TestValue");
            provider.Update(client);
            client = provider.FindClientById("TestUpdate");
            Assert.False(!client.Properties.ContainsKey("TestKey"), "update UpdateClientPropertyByClientID failed");
            var item = client.Properties.FirstOrDefault(c => c.Key == "TestKey");
            Assert.False(item.Value != "TestValue", "update UpdateClientPropertyByClientID failed");
        }

        [Theory]
        [InlineData(xTestBase.MSSQL)]
        [InlineData(xTestBase.MySQL)]
        public void TestQueryAllowedCorsOrigins(string sqltype)
        {
            var provider = GetDefaultClientProvider(sqltype);
            var entity = provider.FindClientById("js_oidc");
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
            var dbitem = provider.FindClientById("js_oauth");
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
