namespace Formula81.XrmToolBox.Libraries.Xrm
{
    public partial class WebResource
    {
        public class ColumnNames
        {
            public static readonly string WebResourceId = GetColumnName<WebResource>(wr => wr.WebResourceId);

            public static readonly string Name = GetColumnName<WebResource>(wr => wr.Name);
            public static readonly string Content = GetColumnName<WebResource>(wr => wr.Content);
            public static readonly string WebResourceType = GetColumnName<WebResource>(wr => wr.WebResourceType);
        }
    }
}
