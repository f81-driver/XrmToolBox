namespace Formula81.XrmToolBox.Shared.XrmParts.Components
{
    public interface IIconifiable
    {
        int? ObjectTypeCode { get; }
        byte[] IconData { get; }
    }
}
