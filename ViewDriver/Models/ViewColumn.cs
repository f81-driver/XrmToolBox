using Formula81.XrmToolBox.Shared.Core.Components;
using System;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    /// <summary>
    /// A cell of the layout: a column of the view, possibly of a related table ("alias.column")
    /// </summary>
    public class ViewColumn : ObservableObject
    {
        public const int MinWidth = 25;
        public const int MaxWidth = 500;
        public const int DefaultWidth = 150;

        /// <summary>Below this width, widths are rounded to steps of <see cref="SmallWidthStep"/>, from it on to steps of <see cref="LargeWidthStep"/></summary>
        public const int LargeWidthThreshold = 200;
        public const int SmallWidthStep = 25;
        public const int LargeWidthStep = 50;

        /// <summary>
        /// The nearest tidy width: a multiple of 25 below 200 (129 to 125, 146 to 150), of 50 from 200 on (226 to 250, 274 to 250),
        /// within the min and max width
        /// </summary>
        public static int RoundWidth(int width)
        {
            var step = width < LargeWidthThreshold ? SmallWidthStep : LargeWidthStep;
            var rounded = (int)Math.Round((double)width / step, MidpointRounding.AwayFromZero) * step;
            return Math.Max(MinWidth, Math.Min(MaxWidth, rounded));
        }

        public string Name { get; }
        public bool IsRelated => Name.Contains(".");

        private int _width;
        public int Width { get => _width; set => SetValue(nameof(Width), Math.Max(MinWidth, Math.Min(MaxWidth, value)), ref _width); }

        private string _displayName;
        public string DisplayName { get => _displayName; set => SetValue(nameof(DisplayName), value, ref _displayName); }

        private string _caption;
        public string Caption { get => _caption; set => SetValue(nameof(Caption), value, ref _caption); }

        // The column is not found in the metadata (deleted, or a related table that is still loading)
        private bool _isUnknown;
        public bool IsUnknown { get => _isUnknown; set => SetValue(nameof(IsUnknown), value, ref _isUnknown); }

        public ViewColumn(string name, int width)
        {
            Name = name;
            _width = Math.Max(MinWidth, Math.Min(MaxWidth, width));
            _displayName = name;
            _caption = name;
        }
    }
}
