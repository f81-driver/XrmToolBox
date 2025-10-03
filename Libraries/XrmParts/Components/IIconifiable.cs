namespace Formula81.XrmToolBox.Libraries.XrmParts.Components
{
    public interface IIconifiable
    {
        int? ObjectTypeCode { get; }
        byte[] IconData { get; }
    }
}
