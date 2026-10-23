using Formula81.XrmToolBox.Shared.Core.Components;
using Formula81.XrmToolBox.Shared.XrmParts.Components;

namespace Formula81.XrmToolBox.Shared.XrmParts.Models
{
    public class EntityItem : ObservableObject, IIconifiable
    {
        public int? ObjectTypeCode { get; }
        public string Name { get; }
        public string DisplayName { get; }
        public byte[] IconData { get; }
        public string ToolTip { get { return $"{DisplayName}\n{Name}"; } }

        private bool _isFavorite;
        public bool IsFavorite { get => _isFavorite; set => SetValue(nameof(IsFavorite), value, ref _isFavorite); }

        public EntityItem(int? objectTypeCode, string name, string displayName, byte[] iconData, bool isFavorite)
        {
            ObjectTypeCode = objectTypeCode;
            Name = name;
            DisplayName = displayName;
            IconData = iconData;
            _isFavorite = isFavorite;
        }
    }
}
