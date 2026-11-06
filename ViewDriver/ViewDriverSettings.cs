using System.Collections.Generic;
using System.Linq;

namespace Formula81.XrmToolBox.Tools.ViewDriver
{
    public class ViewDriverSettings
    {
        public const int DefaultPreviewPageSize = 50;

        private HashSet<string> _favoriteEntitySet;
        private string _favoriteEntities;
        public string FavoriteEntities
        {
            get => _favoriteEntities;
            set
            {
                _favoriteEntities = value;
                _favoriteEntitySet = value?.Split(',').ToHashSet() ?? new HashSet<string>();
            }
        }

        public string EntitySortBy { get; set; }

        /// <summary>Comma separated keys of the checked table criteria, null for the defaults</summary>
        public string EntityCriteria { get; set; }

        /// <summary>The table selected last, to select it again when the tool opens</summary>
        public string SelectedEntity { get; set; }

        /// <summary>Off by default: only personal views are listed</summary>
        public bool ShowSystemViews { get; set; }

        // Stored inverted, so a missing setting means the preview refreshes automatically
        public bool ManualPreviewRefresh { get; set; }

        private int _previewPageSize = DefaultPreviewPageSize;
        public int PreviewPageSize { get => _previewPageSize; set => _previewPageSize = value > 0 && value <= 5000 ? value : DefaultPreviewPageSize; }

        public ViewDriverSettings()
        {
            _favoriteEntitySet = new HashSet<string>();
        }

        public bool IsFavoriteEntity(string logicalName)
        {
            return !string.IsNullOrEmpty(logicalName)
                && _favoriteEntitySet.Contains(logicalName);
        }

        public void FavorizeEntity(string logicalName)
        {
            if (!string.IsNullOrEmpty(logicalName))
            {
                if (!_favoriteEntitySet.Remove(logicalName))
                {
                    _favoriteEntitySet.Add(logicalName);
                }
                _favoriteEntities = string.Join(",", _favoriteEntitySet);
            }
        }
    }
}
