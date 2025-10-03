using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Formula81.XrmToolBox.Libraries.Xrm.Caches
{
    public sealed class EntityIconCache
    {
        private const string ServiceNotInitializedExceptionMessage = "Service Client has not been initialized";

        private static readonly Lazy<EntityIconCache> _lazy =
            new Lazy<EntityIconCache>(() => new EntityIconCache(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static EntityIconCache Instance { get { return _lazy.Value; } }

        private IOrganizationService _serviceClient;
        public IOrganizationService ServiceClient { get => _serviceClient ?? throw new InvalidOperationException(ServiceNotInitializedExceptionMessage); set => _serviceClient = value; }

        private readonly Dictionary<string, byte[]> _entityIconCache;

        private EntityIconCache()
        {
            _entityIconCache = new Dictionary<string, byte[]>();
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
            var uncachedEntityMetadatas = entityMetadatas.Where(em => !_entityIconCache.ContainsKey(em.LogicalName));
            var entityIconDatas = XrmToolkit.GetSmallIconDatas(ServiceClient, uncachedEntityMetadatas); 
            foreach (var uncachedEntityMetadata in uncachedEntityMetadatas.Where(uem => !entityIconDatas.ContainsKey(uem.LogicalName)))
            {
                entityIconDatas[uncachedEntityMetadata.LogicalName] = null;
            }
            foreach (var entityMetadata in entityMetadatas.Where(em => _entityIconCache.ContainsKey(em.LogicalName)))
            {
                entityIconDatas[entityMetadata.LogicalName] = _entityIconCache[entityMetadata.LogicalName];
            }
            return entityIconDatas;
        }

        public bool TryGet(EntityMetadata entityMetadata, out byte[] iconData)
        {
            iconData = Get(entityMetadata);
            return iconData != null;
        }
    }
}
