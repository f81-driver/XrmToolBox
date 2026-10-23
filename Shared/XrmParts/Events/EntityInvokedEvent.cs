using Formula81.XrmToolBox.Shared.XrmParts.Models;

namespace Formula81.XrmToolBox.Shared.XrmParts.Events
{
    /*public class EntityClickedEventArgs : EventArgs
    {
        public EntityItem Entity;
        public EntityClickedEventArgs(EntityItem entity)
        {
            Entity = entity;
        }
    }*/

    public delegate void EntityInvokedEvent(object sender, EntityItem entityItem);
}
