using System.Xml;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Helpers
{
    internal class FetchXmlHelper
    {
        public static string GetLogicalName(string fetchXml)
        {
            var fetchXmlDocument = new XmlDocument();
            fetchXmlDocument.LoadXml(fetchXml);
            var navigator = fetchXmlDocument.CreateNavigator();
            var entityNode = navigator.SelectSingleNode("/fetch/entity");
            return entityNode?.GetAttribute("name", string.Empty);
        }
    }
}
