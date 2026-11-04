namespace Formula81.XrmToolBox.Tools.MetaScanner.Models
{
    public class MetaProperty
    {
        public string Name { get; }
        public string Value { get; }
        public bool IsCopyable { get; }

        public MetaProperty(string name, string value, bool isCopyable = true)
        {
            Name = name;
            Value = value;
            IsCopyable = isCopyable;
        }
    }
}
