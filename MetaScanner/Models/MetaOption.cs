namespace Formula81.XrmToolBox.Tools.MetaScanner.Models
{
    public class MetaOption
    {
        public int? Value { get; }
        public string Label { get; }
        public string Description { get; }
        public string Color { get; }

        public MetaOption(int? value, string label, string description, string color)
        {
            Value = value;
            Label = label;
            Description = description;
            Color = color;
        }
    }
}
