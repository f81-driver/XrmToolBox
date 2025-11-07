using Formula81.XrmToolBox.Tools.AuditGoggles.Components;
using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Mocks
{
    internal class ColorTestItemsSource : IEnumerable<AuditRecord>
    {
        private readonly ReadOnlyCollection<AuditRecord> _auditRecordCollection;

        public ColorTestItemsSource()
        {
            var entities = new List<Tuple<int?, string>>()
            {
                new Tuple<int?, string>(1, "account"),
                new Tuple<int?, string>(2, "contact"),
                new Tuple<int?, string>(3, "lead"),
                new Tuple<int?, string>(4, "opportunity"),
                new Tuple<int?, string>(8, "systemuser"),
                new Tuple<int?, string>(9, "team"),
            };
            var i = 0;
            var auditRecords = new List<AuditRecord>();
            foreach (var cc in ColorCombinations.All)
            {
                var entity = entities[i];
                var name = ColorCombinations.GetColorName(cc.PrimaryBackground);
                auditRecords.Add(new AuditRecord(entity.Item1, Guid.NewGuid(), name, entity.Item2, entity.Item2, null) { ColorCombination = cc});
                i++;
                if (i >= entities.Count)
                {
                    i = 0;
                }
            }
            _auditRecordCollection = auditRecords.AsReadOnly();
        }

        public IEnumerator<AuditRecord> GetEnumerator()
        {
            return _auditRecordCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
