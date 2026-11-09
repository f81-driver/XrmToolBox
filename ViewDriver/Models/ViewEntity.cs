using Formula81.XrmToolBox.Shared.Core.Components;
using Formula81.XrmToolBox.Shared.XrmParts.Components;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    public class ViewEntity : ObservableObject, IIconifiable
    {
        public int? ObjectTypeCode { get; }
        public string LogicalName { get; }
        public string DisplayName { get; }
        public string SchemaName { get; }
        public string Description { get; }
        public bool IsCustom { get; }
        public bool IsActivity { get; }
        public bool IsValidForAdvancedFind { get; }
        public bool IsPrivate { get; }
        public bool IsIntersect { get; }
        /// <summary>Metadata has no flag for it, but Microsoft prefixes the display name of deprecated tables</summary>
        public bool IsDeprecated => DisplayName != null
            && (DisplayName.TrimStart().StartsWith("[Deprecated]", System.StringComparison.OrdinalIgnoreCase)
                || DisplayName.TrimStart().StartsWith("(Deprecated)", System.StringComparison.OrdinalIgnoreCase));
        public byte[] IconData { get; }

        private bool _isFavorite;
        public bool IsFavorite { get => _isFavorite; set => SetValue(nameof(IsFavorite), value, ref _isFavorite); }

        /// <summary>
        /// An invisible entry that keeps the Favorites group (and its header with the hidden count) in the list
        /// while every favorite is hidden. Groups of a collection view cannot be empty.
        /// </summary>
        public bool IsPlaceholder { get; private set; }

        internal static ViewEntity CreateFavoritesPlaceholder()
        {
            return new ViewEntity(null, string.Empty, string.Empty, string.Empty, null, false, false, false, false, false, null, true) { IsPlaceholder = true };
        }

        public string ToolTip => string.IsNullOrEmpty(Description) ? $"{DisplayName}\n{LogicalName}" : $"{DisplayName}\n{LogicalName}\n\n{Description}";

        public ViewEntity(int? objectTypeCode, string logicalName, string displayName, string schemaName, string description,
            bool isCustom, bool isActivity, bool isValidForAdvancedFind, bool isPrivate, bool isIntersect,
            byte[] iconData, bool isFavorite)
        {
            IsValidForAdvancedFind = isValidForAdvancedFind;
            IsPrivate = isPrivate;
            IsIntersect = isIntersect;
            ObjectTypeCode = objectTypeCode;
            LogicalName = logicalName;
            DisplayName = displayName;
            SchemaName = schemaName;
            Description = description;
            IsCustom = isCustom;
            IsActivity = isActivity;
            IconData = iconData;
            _isFavorite = isFavorite;
        }
    }
}
