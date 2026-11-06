using System;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    /// <summary>
    /// User whose personal views are shown; other users than the current one are impersonated
    /// </summary>
    public class ViewOwner
    {
        public Guid Id { get; }
        public string Name { get; }
        public string DomainName { get; }
        public bool IsCurrentUser { get; }

        public string DisplayText => IsCurrentUser ? $"Me ({Name})" : Name;
        public string ToolTip => string.IsNullOrEmpty(DomainName) ? Name : $"{Name}\n{DomainName}";

        public ViewOwner(Guid id, string name, string domainName, bool isCurrentUser)
        {
            Id = id;
            Name = string.IsNullOrEmpty(name) ? id.ToString() : name;
            DomainName = domainName;
            IsCurrentUser = isCurrentUser;
        }

        // Editable combo boxes search and display on this
        public override string ToString()
        {
            return DisplayText;
        }
    }
}
