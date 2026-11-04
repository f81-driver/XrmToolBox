using Formula81.XrmToolBox.Tools.MetaScanner.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Formula81.XrmToolBox.Tools.MetaScanner.Mocks
{
    internal class MetaEntityItemsSource : IEnumerable<MetaEntity>
    {
        private readonly ReadOnlyCollection<MetaEntity> _metaEntityCollection;

        public MetaEntityItemsSource()
        {
            _metaEntityCollection = new List<MetaEntity>
            {
                new MetaEntity(1, Guid.NewGuid(), "account", "Account", "Account", "Business that represents a customer", false, true, false, false, true, null, true),
                new MetaEntity(2, Guid.NewGuid(), "contact", "Contact", "Contact", "Person with whom a business unit has a relationship", false, true, false, false, true, null, false),
                new MetaEntity(10000, Guid.NewGuid(), "nrq_serviceorder", "Service Order", "nrq_ServiceOrder", null, true, false, false, false, false, null, false)
            }.AsReadOnly();
        }

        public IEnumerator<MetaEntity> GetEnumerator()
        {
            return _metaEntityCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
