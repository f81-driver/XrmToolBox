using Formula81.XrmToolBox.Libraries.XrmParts.Components;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Models
{
    public class AuditEntity : IIconifiable
    {
        public int? ObjectTypeCode { get; }
        public string Name { get; }
        public string DisplayName { get; }
        public bool IsAuditEnabled { get; }
        public byte[] IconData { get; }
        public bool IsFavorite { get; set; }
        public string ToolTip { get { return $"{DisplayName}\n{Name}"; } }

        public AuditEntity(int? objectTypeCode, string name, string displayName, bool isAuditEnabled, byte[] iconData, bool isFavorite)
        {
            ObjectTypeCode = objectTypeCode;
            Name = name;
            DisplayName = displayName;
            IsAuditEnabled = isAuditEnabled;
            IconData = iconData;
            IsFavorite = isFavorite;
        }
    }
}
