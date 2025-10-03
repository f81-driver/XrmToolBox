using Formula81.XrmToolBox.Libraries.Core.Components;
using Formula81.XrmToolBox.Libraries.Parts.Components;
using System;
using System.Collections.Generic;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Models
{
    public class EntityAudit : ObservableObject
    {
        public DateTime ChangedDate { get; }
        public string ChangedByName { get; }
        public EntityLookupValue Record { get; }
        public string EventName { get; }
        public IEnumerable<EntityAuditDetail> Details { get; }

        private ColorCombination _colorCombination;
        public ColorCombination ColorCombination { get => _colorCombination; set => SetValue(nameof(ColorCombination), value, ref _colorCombination); }

        public EntityAudit(DateTime changedDate, string changedByName, EntityLookupValue record, string eventName, ColorCombination colorCombination, IEnumerable<EntityAuditDetail> details)
        {
            ChangedDate = changedDate;
            ChangedByName = changedByName;
            Record = record;
            EventName = eventName;
            Details = details;
            _colorCombination = colorCombination;
        }
    }
}
