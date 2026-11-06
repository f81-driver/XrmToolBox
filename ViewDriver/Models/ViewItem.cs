using Formula81.XrmToolBox.Shared.Core.Components;
using System;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    public enum ViewKind
    {
        Personal,
        System
    }

    public class ViewItem : ObservableObject
    {
        public const string PersonalGroupName = "Personal views";
        public const string SystemGroupName = "System views";

        public ViewKind Kind { get; }
        public string EntityLogicalName { get; }
        public int QueryType { get; }
        public bool IsDefault { get; }
        public bool IsInactive { get; }
        public Guid? OwnerId { get; }
        public string OwnerName { get; }
        // A personal view owned by someone else than the user whose views are shown
        public bool IsShared { get; }
        public DateTime? ModifiedOn { get; }

        private Guid? _id;
        public Guid? Id { get => _id; set { SetValue(nameof(Id), value, ref _id); NotifyPropertyChanged(nameof(IsNew)); NotifyPropertyChanged(nameof(Caption)); } }

        private string _name;
        public string Name { get => _name; set { SetValue(nameof(Name), value, ref _name); NotifyPropertyChanged(nameof(ToolTip)); } }

        private string _description;
        public string Description { get => _description; set { SetValue(nameof(Description), value, ref _description); NotifyPropertyChanged(nameof(ToolTip)); } }

        public string FetchXml { get; set; }
        public string LayoutXml { get; set; }

        // Has changes in the editor that are not saved yet
        private bool _isModified;
        public bool IsModified { get => _isModified; set => SetValue(nameof(IsModified), value, ref _isModified); }

        public bool IsNew => Id == null;
        public bool IsPersonal => Kind == ViewKind.Personal;
        public string GroupName => Kind == ViewKind.Personal ? PersonalGroupName : SystemGroupName;
        public string QueryTypeName => GetQueryTypeName(Kind, QueryType);

        public string Caption
        {
            get
            {
                if (IsNew)
                {
                    return "New, not saved yet";
                }
                var caption = Kind == ViewKind.Personal && IsShared ? $"Shared by {OwnerName}" : QueryTypeName;
                if (IsDefault)
                {
                    caption += " · Default";
                }
                if (IsInactive)
                {
                    caption += " · Inactive";
                }
                return caption;
            }
        }

        public string ToolTip => string.IsNullOrEmpty(Description) ? $"{Name}\n{Caption}" : $"{Name}\n{Caption}\n\n{Description}";

        public ViewItem(ViewKind kind, Guid? id, string entityLogicalName, string name, string description, string fetchXml, string layoutXml,
            int queryType, bool isDefault, bool isInactive, Guid? ownerId, string ownerName, bool isShared, DateTime? modifiedOn)
        {
            Kind = kind;
            _id = id;
            EntityLogicalName = entityLogicalName;
            _name = name;
            _description = description;
            FetchXml = fetchXml;
            LayoutXml = layoutXml;
            QueryType = queryType;
            IsDefault = isDefault;
            IsInactive = isInactive;
            OwnerId = ownerId;
            OwnerName = ownerName;
            IsShared = isShared;
            ModifiedOn = modifiedOn;
        }

        public static string GetQueryTypeName(ViewKind kind, int queryType)
        {
            if (kind == ViewKind.Personal)
            {
                return "Personal";
            }
            switch (queryType)
            {
                case 0: return "Public";
                case 1: return "Advanced Find";
                case 2: return "Associated";
                case 4: return "Quick Find";
                case 64: return "Lookup";
                default: return $"Type {queryType}";
            }
        }
    }
}
