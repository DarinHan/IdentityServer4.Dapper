using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer4.Dapper.Interfaces
{
    public interface IPersistedGrantProvider
    {
        IEnumerable<PersistedGrant> GetAll(string subjectId);
        IEnumerable<PersistedGrant> GetAll(string subjectId, string clientId);
        IEnumerable<PersistedGrant> GetAll(string subjectId, string clientId, string type);
        PersistedGrant Get(string key);
        void Add(PersistedGrant token);
        void Update(PersistedGrant token);
        void RemoveAll(string subjectId, string clientId);
        void RemoveAll(string subjectId, string clientId, string type);
        void Remove(string key);
        void Store(PersistedGrant grant);
    }
}
