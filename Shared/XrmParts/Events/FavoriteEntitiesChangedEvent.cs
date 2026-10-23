using Formula81.XrmToolBox.Shared.XrmParts.Exceptions;
using Formula81.XrmToolBox.Shared.XrmParts.Models;
using System;
using System.Collections.Generic;

namespace Formula81.XrmToolBox.Shared.XrmParts.Events
{
    public class FavoriteEntitiesChangedArgs : EventArgs
    {
        public IEnumerable<EntityItem> Entities { get; }
        public FavoriteEntitiesChangedArgs(IEnumerable<EntityItem> entities, FavorizeEntityException error = null)
        {
            Entities = entities;
        }
    }

    public delegate void FavoriteEntitiesChangedEvent(object sender, FavoriteEntitiesChangedArgs args);
}
