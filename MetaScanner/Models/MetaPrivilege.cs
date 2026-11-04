namespace Formula81.XrmToolBox.Tools.MetaScanner.Models
{
    public class MetaPrivilege
    {
        public string Name { get; }
        public string Type { get; }
        public bool CanBeBasic { get; }
        public bool CanBeLocal { get; }
        public bool CanBeDeep { get; }
        public bool CanBeGlobal { get; }

        public MetaPrivilege(string name, string type, bool canBeBasic, bool canBeLocal, bool canBeDeep, bool canBeGlobal)
        {
            Name = name;
            Type = type;
            CanBeBasic = canBeBasic;
            CanBeLocal = canBeLocal;
            CanBeDeep = canBeDeep;
            CanBeGlobal = canBeGlobal;
        }
    }
}
