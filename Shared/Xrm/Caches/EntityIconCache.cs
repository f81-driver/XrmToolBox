using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Formula81.XrmToolBox.Shared.Xrm.Caches
{
    public sealed class EntityIconCache
    {
        private const string ServiceNotInitializedExceptionMessage = "Service Client has not been initialized";

        private static readonly Lazy<EntityIconCache> _lazy =
            new Lazy<EntityIconCache>(() => new EntityIconCache(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static EntityIconCache Instance { get { return _lazy.Value; } }

        private IOrganizationService _serviceClient;
        public IOrganizationService ServiceClient { get => _serviceClient ?? throw new InvalidOperationException(ServiceNotInitializedExceptionMessage); set => _serviceClient = value; }

        private readonly ConcurrentDictionary<string, byte[]> _entityIconCache;

        private EntityIconCache()
        {
            _entityIconCache = new ConcurrentDictionary<string, byte[]>();
        }

        public byte[] Get(EntityMetadata entityMetadata)
        {
            if (!_entityIconCache.TryGetValue(entityMetadata.LogicalName, out var iconData))
            {
                iconData = XrmToolkit.GetSmallIconData(ServiceClient, entityMetadata);
                _entityIconCache[entityMetadata.LogicalName] = iconData;
            }
            return iconData;
        }

        public IDictionary<string, byte[]> GetMany(IEnumerable<EntityMetadata> entityMetadatas)
        {
            var entityMetadataList = entityMetadatas.ToList();
            var uncachedEntityMetadatas = entityMetadataList.Where(em => !_entityIconCache.ContainsKey(em.LogicalName)).ToList();
            if (uncachedEntityMetadatas.Any())
            {
                var uncachedIconDatas = XrmToolkit.GetSmallIconDatas(ServiceClient, uncachedEntityMetadatas);
                foreach (var uncachedEntityMetadata in uncachedEntityMetadatas)
                {
                    _entityIconCache[uncachedEntityMetadata.LogicalName] = uncachedIconDatas.TryGetValue(uncachedEntityMetadata.LogicalName, out var iconData) ? iconData : null;
                }
            }
            var entityIconDatas = new Dictionary<string, byte[]>();
            foreach (var entityMetadata in entityMetadataList)
            {
                entityIconDatas[entityMetadata.LogicalName] = _entityIconCache.TryGetValue(entityMetadata.LogicalName, out var iconData) ? iconData : null;
            }
            return entityIconDatas;
        }

        public void Clear()
        {
            _entityIconCache.Clear();
        }

        public bool TryGet(EntityMetadata entityMetadata, out byte[] iconData)
        {
            iconData = Get(entityMetadata);
            return iconData != null;
        }
    }
}
