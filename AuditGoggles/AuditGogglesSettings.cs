using System;
using System.Collections.Generic;
using System.Linq;

namespace Formula81.XrmToolBox.Tools.AuditGoggles
{
    public class AuditGogglesSettings
    {
        private HashSet<string> _favoriteAuditEntitySet;
        private string _favoriteAuditEntities;
        public string FavoriteAuditEntities
        {
            get => _favoriteAuditEntities;
            set
            {
                _favoriteAuditEntities = value;
                _favoriteAuditEntitySet = value?.Split(',').ToHashSet() ?? new HashSet<string>();
            }
        }

        public AuditGogglesSettings()
        {
            _favoriteAuditEntitySet = new HashSet<string>();
        }

        public bool IsFavoriteAuditEntity(string logicalName)
        {
            return !string.IsNullOrEmpty(logicalName)
                && _favoriteAuditEntitySet.Contains(logicalName);
        }

        public void FavorizeAuditEntities(string logicalName)
        {
            if (!string.IsNullOrEmpty(logicalName))
            {
                if (_favoriteAuditEntitySet.Contains(logicalName))
                {
                    _favoriteAuditEntitySet.Remove(logicalName);
                }
                else
                {
                    _favoriteAuditEntitySet.Add(logicalName);
                }
                _favoriteAuditEntities = string.Join(",", _favoriteAuditEntitySet);
            }
        }
    }
}