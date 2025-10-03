using Formula81.XrmToolBox.Libraries.Core.Components;
using Formula81.XrmToolBox.Libraries.Parts.Components;
using Formula81.XrmToolBox.Libraries.XrmParts.Components;
using System;
using System.Windows.Input;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Models
{
    public class AuditRecord : ObservableObject, IIconifiable
    {
        public int? ObjectTypeCode { get; }
        public Guid Id { get; }
        public string Name { get; }
        public string EntityLogicalName { get; }
        public string EntityDisplayName { get; }
        public byte[] IconData { get; }

        private ColorCombination _colorCombination;
        public ColorCombination ColorCombination { get => _colorCombination; set => SetValue(nameof(ColorCombination), value, ref _colorCombination); }

        public ICommand ChangeColorCommand { get; }
        public ICommand RemoveCommand { get; }

        public AuditRecord(int? objectTypeCode, Guid id, string name, string entityLogicalName, string entityDisplayName, byte[] iconData)
        {
            ObjectTypeCode = objectTypeCode;
            Id = id;
            Name = name;
            EntityLogicalName = entityLogicalName;
            EntityDisplayName = entityDisplayName;
            IconData = iconData;
        }
    }
}
