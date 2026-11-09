using Formula81.XrmToolBox.Shared.Core.Components;
using Microsoft.Crm.Sdk.Messages;
using System;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    /// <summary>
    /// A user or team a view can be shared with
    /// </summary>
    public class SharePrincipal
    {
        public const string UserLogicalName = "systemuser";
        public const string TeamLogicalName = "team";

        public Guid Id { get; }
        public string LogicalName { get; }
        public string Name { get; }
        /// <summary>The domain name of a user, the kind of team of a team</summary>
        public string Detail { get; }

        public bool IsTeam => LogicalName == TeamLogicalName;
        public string KindText => IsTeam ? "Team" : "User";
        public string ToolTip => string.IsNullOrEmpty(Detail) ? $"{Name}\n{KindText}" : $"{Name}\n{KindText} · {Detail}";

        public SharePrincipal(Guid id, string logicalName, string name, string detail)
        {
            Id = id;
            LogicalName = logicalName;
            Name = string.IsNullOrEmpty(name) ? id.ToString() : name;
            Detail = detail;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// A user or team the view is shared with, and the access it has
    /// </summary>
    public class ShareEntry : ObservableObject
    {
        /// <summary>The rights this dialog lets people change; others the share has (such as append) are kept as they are</summary>
        public const AccessRights EditableRights = AccessRights.ReadAccess | AccessRights.WriteAccess | AccessRights.DeleteAccess
            | AccessRights.ShareAccess | AccessRights.AssignAccess;

        public SharePrincipal Principal { get; }

        /// <summary>The access the share has in Dataverse, null for a share that is not saved yet</summary>
        public AccessRights? OriginalRights { get; }

        public bool IsNew => OriginalRights == null;

        private bool _canRead;
        public bool CanRead
        {
            get => _canRead;
            set
            {
                SetValue(nameof(CanRead), value, ref _canRead);
                // Without read the other rights mean nothing
                if (!value)
                {
                    CanWrite = CanDelete = CanShare = CanAssign = false;
                }
            }
        }

        private bool _canWrite;
        public bool CanWrite { get => _canWrite; set { SetValue(nameof(CanWrite), value, ref _canWrite); EnsureRead(value); } }

        private bool _canDelete;
        public bool CanDelete { get => _canDelete; set { SetValue(nameof(CanDelete), value, ref _canDelete); EnsureRead(value); } }

        private bool _canShare;
        public bool CanShare { get => _canShare; set { SetValue(nameof(CanShare), value, ref _canShare); EnsureRead(value); } }

        private bool _canAssign;
        public bool CanAssign { get => _canAssign; set { SetValue(nameof(CanAssign), value, ref _canAssign); EnsureRead(value); } }

        public ShareEntry(SharePrincipal principal, AccessRights? originalRights)
        {
            Principal = principal;
            OriginalRights = originalRights;
            var rights = originalRights ?? AccessRights.ReadAccess;
            _canRead = rights.HasFlag(AccessRights.ReadAccess);
            _canWrite = rights.HasFlag(AccessRights.WriteAccess);
            _canDelete = rights.HasFlag(AccessRights.DeleteAccess);
            _canShare = rights.HasFlag(AccessRights.ShareAccess);
            _canAssign = rights.HasFlag(AccessRights.AssignAccess);
        }

        /// <summary>The access to save: the checked rights, plus the rights the dialog does not show</summary>
        public AccessRights GetRights()
        {
            var rights = (OriginalRights ?? AccessRights.None) & ~EditableRights;
            if (CanRead) rights |= AccessRights.ReadAccess;
            if (CanWrite) rights |= AccessRights.WriteAccess;
            if (CanDelete) rights |= AccessRights.DeleteAccess;
            if (CanShare) rights |= AccessRights.ShareAccess;
            if (CanAssign) rights |= AccessRights.AssignAccess;
            return rights;
        }

        private void EnsureRead(bool value)
        {
            if (value && !_canRead)
            {
                CanRead = true;
            }
        }
    }
}
