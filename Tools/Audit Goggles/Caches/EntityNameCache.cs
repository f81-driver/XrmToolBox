using Formula81.XrmToolBox.Libraries.Core.Extensions;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Caches
{
    public class EntityNameCache
    {
        private const string ServiceNotInitializedExceptionMessage = "Service Client has not been initialized";

        private static readonly Lazy<EntityNameCache> _lazy =
            new Lazy<EntityNameCache>(() => new EntityNameCache(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static EntityNameCache Instance { get { return _lazy.Value; } }

        private CrmServiceClient _serviceClient;
        public CrmServiceClient ServiceClient { get => _serviceClient ?? throw new InvalidOperationException(ServiceNotInitializedExceptionMessage); set { _serviceClient = value; } }

        private readonly Dictionary<string, Dictionary<Guid, string>> _entityNameCache;

        private EntityNameCache()
        {
            _entityNameCache = new Dictionary<string, Dictionary<Guid, string>>();
        }

        public IEnumerable<Guid> Uncached(string logicalName, IEnumerable<Guid> ids)
        {
            var entityNames = GetEntityNames(logicalName);
            return ids.Where(i => !entityNames.ContainsKey(i));
        }

        public void Upsert(string logicalName, IDictionary<Guid, string> upsertEntityNames)
        {
            var entityNames = GetEntityNames(logicalName);
            foreach (var upsertEntityName in upsertEntityNames)
            {
                entityNames[upsertEntityName.Key] = upsertEntityName.Value;
            }
        }

        public string Get(string logicalName, Guid id)
        {
            var entityNames = GetEntityNames(logicalName);
            if (!entityNames.TryGetValue(id, out var name))
            {
                name = GetEntityName(logicalName, id);
            }
            return name;
        }

        public IDictionary<Guid, string> GetMany(string logicalName, IEnumerable<Guid> ids)
        {
            var uncached = Uncached(logicalName, ids);
            if (uncached.Any())
            {
                CacheEntities(logicalName, uncached);
            }
            var entityNames = GetEntityNames(logicalName);
            var idSet = new HashSet<Guid>(ids);
            return entityNames.Where(en => idSet.Contains(en.Key))
                .ToDictionary(en => en.Key, en => en.Value);
        }

        private Dictionary<Guid, string> GetEntityNames(string logicalName)
        {
            if (!_entityNameCache.TryGetValue(logicalName, out var entityNames))
            {
                entityNames = new Dictionary<Guid, string>();
                _entityNameCache[logicalName] = entityNames;
            }
            return entityNames;
        }

        private string GetEntityName(string logicalName, Guid id)
        {
            var entityMetadata = ServiceClient.GetEntityMetadata(logicalName);
            var entity = ServiceClient.Retrieve(logicalName, id, new ColumnSet(entityMetadata.PrimaryNameAttribute));
            var name = entity?.GetAttributeValue<string>(entityMetadata.PrimaryNameAttribute);
            var entityNames = GetEntityNames(logicalName);
            entityNames[id] = name;
            return name;
        }

        private void CacheEntities(string logicalName, IEnumerable<Guid> ids)
        {
            var idQueue = new Queue<Guid>(ids);
            var entityMetadata = ServiceClient.GetEntityMetadata(logicalName);
            var entityNames = GetEntityNames(logicalName);
            while (idQueue.Count > 0)
            {
                var idChunk = idQueue.DequeueChunk(100);
                var query = new QueryExpression(entityMetadata.LogicalName)
                {
                    ColumnSet = new ColumnSet(entityMetadata.PrimaryIdAttribute, entityMetadata.PrimaryNameAttribute),
                    Criteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression(entityMetadata.PrimaryIdAttribute, ConditionOperator.In, idChunk.ToArray())
                            }
                        }
                };
                var entities = ServiceClient.RetrieveMultiple(query);
                foreach (var entity in entities.Entities)
                {
                    entityNames[entity.Id] = entity.GetAttributeValue<string>(entityMetadata.PrimaryNameAttribute);
                }
            }
        }
    }
}