using IdentityServer4.Dapper.DefaultProviders;
using IdentityServer4.Dapper.Extensions;
using IdentityServer4.Dapper.Options;
using IdentityServer4.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace IdentityServer.Dapper.xUnitTest
{
    public class xTestDefaultPersistedGrantProvider
    {
        private DefaultPersistedGrantProvider GetDefaultPersistedGrantProvider()
        {
            DBProviderOptions options = IdentityServerDapperDBExtensions.GetDefaultOptions();
            options.ConnectionString = xTestBase.ConnectionString;
            return new DefaultPersistedGrantProvider(options, null);
        }

        [Fact]
        public void TestAddUpdateGet()
        {
            var provider = GetDefaultPersistedGrantProvider();
            var grant = new PersistedGrant()
            {
                Key = "TestAddKey",
                Type = "TestType",
                SubjectId = "SubjectId",
                ClientId = "ClientId",
                Data = "Data",
                CreationTime = DateTime.Now,
                Expiration = DateTime.Now.AddHours(1)
            };
            var dbitem = provider.Get(grant.Key);
            if (dbitem == null)
            {
                provider.Add(grant);
            }
            dbitem = provider.Get(grant.Key);
            Assert.False(dbitem == null);

            var datetime = DateTime.Now.AddHours(1);
            dbitem.Expiration = datetime;
            provider.Update(dbitem);
            dbitem = provider.Get(grant.Key);

            Assert.False(dbitem == null || dbitem.Expiration.Value.Hour != datetime.Hour || dbitem.Expiration.Value.Minute != datetime.Minute, "Expiration not match");
        }

        [Fact]
        public void TestGetAll()
        {
            var provider = GetDefaultPersistedGrantProvider();
            var grant = new PersistedGrant()
            {
                Key = "TestAddKey",
                Type = "TestType",
                SubjectId = "SubjectId",
                ClientId = "ClientId",
                Data = "Data",
                CreationTime = DateTime.Now,
                Expiration = DateTime.Now.AddHours(1)
            };
            var dbitem = provider.Get(grant.Key);
            if (dbitem == null)
            {
                provider.Add(grant);
            }

            var result = provider.GetAll(grant.SubjectId);
            Assert.False(result == null || result.Count() == 0);
            result = provider.GetAll(grant.SubjectId, grant.ClientId);
            Assert.False(result == null || result.Count() == 0);
            result = provider.GetAll(grant.SubjectId, grant.ClientId, grant.Type);
            Assert.False(result == null || result.Count() == 0);

            dbitem = result.FirstOrDefault(c => c.Key == grant.Key);
            Assert.False(dbitem == null);
        }

        [Fact]
        public void TestExpired()
        {
            var provider = GetDefaultPersistedGrantProvider();
            var grant = new PersistedGrant()
            {
                Key = "TestExpired",
                Type = "TestType",
                SubjectId = "SubjectId",
                ClientId = "ClientId",
                Data = "Data",
                CreationTime = DateTime.Now,
                Expiration = DateTime.Now.AddHours(-1)
            };
            var dbitem = provider.Get(grant.Key);
            if (dbitem == null)
            {
                provider.Add(grant);
            }
            else
            {
                dbitem.Expiration = grant.Expiration;
                provider.Update(dbitem);
            }

            var erxpired = provider.QueryExpired(DateTime.Now);
            Assert.False(erxpired <= 0);

            provider.RemoveRange(DateTime.Now);
            erxpired = provider.QueryExpired(DateTime.Now);
            Assert.False(erxpired > 0);
        }

        [Fact]
        public void TestRemove()
        {
            var provider = GetDefaultPersistedGrantProvider();
            AddRemoveData(provider);
            provider.Remove("TestRemove");
            var removed = provider.Get("TestRemove");
            Assert.False(removed != null);

            AddRemoveData(provider);
            provider.RemoveAll("TestRemoveSubjectId", "TestRemoveClientId");
            removed = provider.Get("TestRemove");
            Assert.False(removed != null);

            AddRemoveData(provider);
            provider.RemoveAll("TestRemoveSubjectId", "TestRemoveClientId", "TestRemoveType");
            removed = provider.Get("TestRemove");
            Assert.False(removed != null);
        }

        [Fact]
        public void TestSearch()
        {
            var provider = GetDefaultPersistedGrantProvider();
            var grant = new PersistedGrant()
            {
                Key = "TestSearch",
                Type = "TestSearchType",
                SubjectId = "TestSearchSubjectId",
                ClientId = "TestSearchClientId",
                Data = "TestSearchData",
                CreationTime = DateTime.Now,
                Expiration = DateTime.Now.AddHours(-1)
            };
            var dbitem = provider.Get(grant.Key);
            if (dbitem == null)
            {
                provider.Add(grant);
            }
            else
            {
                dbitem.Expiration = grant.Expiration;
                provider.Update(dbitem);
            }
            int totalcount = 0;
            var result = provider.Search(grant.Key, 1, 10, out totalcount);
            Assert.False(totalcount <= 0, "total count <= 0");
            Assert.False(result == null || result.Count() == 0, "result is empty");

            result = provider.Search(grant.ClientId, 1, 10, out totalcount);
            Assert.False(totalcount <= 0, "total count <= 0");
            Assert.False(result == null || result.Count() == 0, "result is empty");

            result = provider.Search(grant.SubjectId, 1, 10, out totalcount);
            Assert.False(totalcount <= 0, "total count <= 0");
            Assert.False(result == null || result.Count() == 0, "result is empty");
        }

        private void AddRemoveData(DefaultPersistedGrantProvider provider)
        {
            var grant = new PersistedGrant()
            {
                Key = "TestRemove",
                Type = "TestRemoveType",
                SubjectId = "TestRemoveSubjectId",
                ClientId = "TestRemoveClientId",
                Data = "TestRemoveData",
                CreationTime = DateTime.Now,
                Expiration = DateTime.Now.AddHours(-1)
            };
            var dbitem = provider.Get(grant.Key);
            if (dbitem == null)
            {
                provider.Add(grant);
            }
            else
            {
                dbitem.Expiration = grant.Expiration;
                provider.Update(dbitem);
            }
        }
    }
}
