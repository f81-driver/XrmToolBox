using Formula81.XrmToolBox.Shared.Core.Components;
using Formula81.XrmToolBox.Shared.XrmParts.Components;
using System;

namespace Formula81.XrmToolBox.Tools.MetaScanner.Models
{
    public class MetaEntity : ObservableObject, IIconifiable
    {
        public int? ObjectTypeCode { get; }
        public Guid? MetadataId { get; }
        public string LogicalName { get; }
        public string DisplayName { get; }
        public string SchemaName { get; }
        public string Description { get; }
        public bool IsCustom { get; }
        public bool IsManaged { get; }
        public bool IsActivity { get; }
        public bool IsIntersect { get; }
        public bool IsAuditEnabled { get; }
        public byte[] IconData { get; }

        private bool _isFavorite;
        public bool IsFavorite { get => _isFavorite; set => SetValue(nameof(IsFavorite), value, ref _isFavorite); }

        public string ToolTip => string.IsNullOrEmpty(Description) ? $"{DisplayName}\n{LogicalName}" : $"{DisplayName}\n{LogicalName}\n\n{Description}";

        public MetaEntity(int? objectTypeCode, Guid? metadataId, string logicalName, string displayName, string schemaName, string description,
            bool isCustom, bool isManaged, bool isActivity, bool isIntersect, bool isAuditEnabled, byte[] iconData, bool isFavorite)
        {
            ObjectTypeCode = objectTypeCode;
            MetadataId = metadataId;
            LogicalName = logicalName;
            DisplayName = displayName;
            SchemaName = schemaName;
            Description = description;
            IsCustom = isCustom;
            IsManaged = isManaged;
            IsActivity = isActivity;
            IsIntersect = isIntersect;
            IsAuditEnabled = isAuditEnabled;
            IconData = iconData;
            _isFavorite = isFavorite;
        }
    }
}
