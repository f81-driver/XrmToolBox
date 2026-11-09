namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    public class ViewOption
    {
        // FetchXML condition value, so a string
        public string Value { get; }
        public string Label { get; }

        public string DisplayText => $"{Label} ({Value})";

        public ViewOption(string value, string label)
        {
            Value = value;
            Label = label;
        }
    }
}
