namespace Formula81.XrmToolBox.Tools.MetaScanner.Models
{
    public enum MetaBadgeKind
    {
        Default,
        Custom,
        System,
        Managed,
        Unmanaged,
        AuditEnabled,
        AuditDisabled
    }

    public class MetaBadge
    {
        public string Text { get; }
        public MetaBadgeKind Kind { get; }

        public MetaBadge(string text, MetaBadgeKind kind = MetaBadgeKind.Default)
        {
            Text = text;
            Kind = kind;
        }
    }
}
