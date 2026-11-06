using Formula81.XrmToolBox.Shared.Core.Components;
using System;
using System.Xml.Linq;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    /// <summary>
    /// A sort order of the view table, edited directly on its FetchXML element
    /// </summary>
    public class ViewOrder : ObservableObject
    {
        private const string AttributeAttributeName = "attribute";
        private const string DescendingAttributeName = "descending";

        public event EventHandler Changed;

        public XElement Element { get; }

        public string AttributeName
        {
            get => (string)Element.Attribute(AttributeAttributeName);
            set
            {
                if (string.IsNullOrEmpty(value) || value == AttributeName)
                {
                    return;
                }
                Element.SetAttributeValue(AttributeAttributeName, value);
                NotifyPropertyChanged(nameof(AttributeName));
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        // 0 = ascending, 1 = descending, bound to the selected index of a combo box
        public int DirectionIndex
        {
            get => IsDescending ? 1 : 0;
            set
            {
                if (value == DirectionIndex || value < 0)
                {
                    return;
                }
                Element.SetAttributeValue(DescendingAttributeName, value == 1 ? "true" : "false");
                NotifyPropertyChanged(nameof(DirectionIndex));
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsDescending => string.Equals((string)Element.Attribute(DescendingAttributeName), "true", StringComparison.OrdinalIgnoreCase);

        public ViewOrder(XElement element)
        {
            Element = element;
        }
    }
}
