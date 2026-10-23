using System.Collections.Generic;
using System.Linq;

namespace Formula81.XrmToolBox.Tools.ViewDriver
{
    public class ViewDriverSettings
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

        public ViewDriverSettings()
        {
            _favoriteEntitySet = new HashSet<string>();
        }

        public bool IsFavoriteEntity(string logicalName)
        {
            return !string.IsNullOrEmpty(logicalName)
                && _favoriteEntitySet.Contains(logicalName);
        }
    }
}