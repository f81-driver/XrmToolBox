using Formula81.XrmToolBox.Shared.Core.Components;
using System;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    /// <summary>
    /// One checkbox of the table filter dropdown
    /// </summary>
    public class TableCriterion : ObservableObject
    {
        /// <summary>Stored in the settings, so never change it</summary>
        public string Key { get; }
        public string Label { get; }
        public string ToolTip { get; }

        /// <summary>
        /// A requirement must be met by every table shown. The other criteria are kinds of tables,
        /// and a table is shown when it is of any checked kind (or of any kind when none is checked).
        /// </summary>
        public bool IsRequirement { get; }

        /// <summary>
        /// A requirement that applies while unchecked instead of while checked: checking it lets in the tables it would keep out
        /// </summary>
        public bool AppliesWhenUnchecked { get; }

        /// <summary>Whether the criterion takes part in filtering now</summary>
        public bool Applies => IsChecked != AppliesWhenUnchecked;

        /// <summary>How the criterion reads in the summary of the dropdown while it applies</summary>
        public string SummaryLabel => AppliesWhenUnchecked ? $"Not {Label.ToLowerInvariant()}" : Label;

        private readonly Func<ViewEntity, bool> _isMatch;

        private bool _isChecked;
        public bool IsChecked { get => _isChecked; set => SetValue(nameof(IsChecked), value, ref _isChecked); }

        private bool _isEnabled = true;
        /// <summary>False while All tables locks the criteria</summary>
        public bool IsEnabled { get => _isEnabled; set => SetValue(nameof(IsEnabled), value, ref _isEnabled); }

        public TableCriterion(string key, string label, string toolTip, bool isRequirement, Func<ViewEntity, bool> isMatch, bool appliesWhenUnchecked = false)
        {
            Key = key;
            Label = label;
            ToolTip = toolTip;
            IsRequirement = isRequirement;
            AppliesWhenUnchecked = appliesWhenUnchecked;
            _isMatch = isMatch;
        }

        public bool IsMatch(ViewEntity viewEntity)
        {
            return _isMatch(viewEntity);
        }
    }
}
