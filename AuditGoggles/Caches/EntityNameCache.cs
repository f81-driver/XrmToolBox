using Formula81.XrmToolBox.Shared.Core.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
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

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, string>> _entityNameCache;
        // Ids that were looked up but do not exist (anymore), so they are not retrieved again
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, bool>> _missingIdCache;

        private EntityNameCache()
        {
            _entityNameCache = new ConcurrentDictionary<string, ConcurrentDictionary<Guid, string>>();
            _missingIdCache = new ConcurrentDictionary<string, ConcurrentDictionary<Guid, bool>>();
        }

        public IEnumerable<Guid> Uncached(string logicalName, IEnumerable<Guid> ids)
        {
            var entityNames = GetEntityNames(logicalName);
            var missingIds = GetMissingIds(logicalName);
            return ids.Where(i => !entityNames.ContainsKey(i) && !missingIds.ContainsKey(i))
                .Distinct()
                .ToList();
        }

        public void Upsert(string logicalName, IDictionary<Guid, string> upsertEntityNames)
        {
            var entityNames = GetEntityNames(logicalName);
            var missingIds = GetMissingIds(logicalName);
            foreach (var upsertEntityName in upsertEntityNames)
            {
                entityNames[upsertEntityName.Key] = upsertEntityName.Value;
                missingIds.TryRemove(upsertEntityName.Key, out _);
            }
        }

        public string Get(string logicalName, Guid id)
        {
            if (!GetEntityNames(logicalName).TryGetValue(id, out var name)
                && !GetMissingIds(logicalName).ContainsKey(id))
            {
                CacheEntities(logicalName, new[] { id });
                GetEntityNames(logicalName).TryGetValue(id, out name);
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

        public void Prefetch(IEnumerable<EntityReference> entityRefs)
        {
            foreach (var entityRefGroup in entityRefs.Where(er => er != null && !string.IsNullOrEmpty(er.LogicalName))
                .GroupBy(er => er.LogicalName))
            {
                var uncached = Uncached(entityRefGroup.Key, entityRefGroup.Select(er => er.Id));
                if (uncached.Any())
                {
                    try
                    {
                        CacheEntities(entityRefGroup.Key, uncached);
                    }
                    catch (FaultException<OrganizationServiceFault>)
                    {
                        // Prefetching is an optimization only, names that failed are retrieved on demand by Get
                    }
                }
            }
        }

        public void Clear()
        {
            _entityNameCache.Clear();
            _missingIdCache.Clear();
        }

        private ConcurrentDictionary<Guid, string> GetEntityNames(string logicalName)
        {
            return _entityNameCache.GetOrAdd(logicalName, _ => new ConcurrentDictionary<Guid, string>());
        }

        private ConcurrentDictionary<Guid, bool> GetMissingIds(string logicalName)
        {
            return _missingIdCache.GetOrAdd(logicalName, _ => new ConcurrentDictionary<Guid, bool>());
        }

        private void CacheEntities(string logicalName, IEnumerable<Guid> ids)
        {
            var idQueue = new Queue<Guid>(ids);
            var entityMetadata = ServiceClient.GetEntityMetadata(logicalName);
            var entityNames = GetEntityNames(logicalName);
            var missingIds = GetMissingIds(logicalName);
            while (idQueue.Count > 0)
            {
                var idChunk = idQueue.DequeueChunk(100).ToList();
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
                foreach (var missingId in idChunk.Where(i => !entityNames.ContainsKey(i)))
                {
                    missingIds[missingId] = true;
                }
            }
        }
    }
}
