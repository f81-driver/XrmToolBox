using System.Collections.Generic;
using System.Linq;

namespace Formula81.XrmToolBox.Tools.MetaScanner
{
    public class MetaScannerSettings
    {
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

        public bool ShowHelperColumns { get; set; }

        public string EntitySortBy { get; set; }

        public MetaScannerSettings()
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
