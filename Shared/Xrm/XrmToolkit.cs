using Formula81.XrmToolBox.Shared.Core.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Formula81.XrmToolBox.Shared.Xrm
{
    public class XrmToolkit
    {
        public static IDictionary<string, byte[]> GetSmallIconDatas(IOrganizationService service, IEnumerable<EntityMetadata> entityMetadatas)
        {
            var datas = new Dictionary<string, byte[]>();
            var iconSmallNames = entityMetadatas.Where(em => !string.IsNullOrEmpty(em.IconSmallName));
            var logicalNamesByIconSmallName = iconSmallNames.GroupBy(em => em.IconSmallName)
                .ToDictionary(g => g.Key, g => g.Select(em => em.LogicalName));
            var iconSmallNameQueue = new Queue<string>(logicalNamesByIconSmallName.Keys);
            while (iconSmallNameQueue.Count > 0)
            {
                var iconSmallNameChunk = iconSmallNameQueue.DequeueChunk(100);
                var query = new QueryExpression(WebResource.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(WebResource.ColumnNames.Name, WebResource.ColumnNames.Content),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression(WebResource.ColumnNames.Name, ConditionOperator.In, iconSmallNameChunk.ToArray()),
                            new ConditionExpression(WebResource.ColumnNames.WebResourceType, ConditionOperator.NotEqual, 11) // Vector format (SVG), not supported at this moment
                        }
                    }
                };
                var webresources = service.RetrieveMultiple(query);
                foreach (var webresource in webresources.Entities.Select(e => e.ToEntity<WebResource>()))
                {
                    var name = webresource.GetAttributeValue<string>(WebResource.ColumnNames.Name);
                    var content = webresource.GetAttributeValue<string>(WebResource.ColumnNames.Content);
                    var data = Convert.FromBase64String(content);
                    foreach (var logicalName in logicalNamesByIconSmallName[name])
                    {
                        datas[logicalName] = data;
                    }
                }
            }
            return datas;
        }

        public static byte[] GetSmallIconData(IOrganizationService service, EntityMetadata entityMetadata/*, string crmHostUrl, bool useWebClient = false*/)
        {
            byte[] iconData = null;
            if ((entityMetadata.IsCustomEntity ?? false) && !string.IsNullOrEmpty(entityMetadata.IconSmallName))
            {
                var query = new QueryExpression(WebResource.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(WebResource.ColumnNames.Content),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression(WebResource.ColumnNames.Name, ConditionOperator.Equal, entityMetadata.IconSmallName),
                            new ConditionExpression(WebResource.ColumnNames.WebResourceType, ConditionOperator.NotEqual, 11) // Vector format (SVG), not supported at this moment
                        }
                    }
                };
                var webresources = service.RetrieveMultiple(query);
                var content = webresources?.Entities?.FirstOrDefault()?.GetAttributeValue<string>(WebResource.ColumnNames.Content);
                if (!string.IsNullOrEmpty(content))
                {
                    iconData = Convert.FromBase64String(content);
                }
            }
            /*if (iconData == null && useWebClient)
            {
                using (var client = new WebClient())
                {
                    try
                    {
                        iconData = client.DownloadData(GetSystemEntityIconPath(entityMetadata.ObjectTypeCode.Value.ToString(), entityMetadata.IsCustomEntity ?? false, crmHostUrl));
                    }
                    catch (Exception)
                    {
                        iconData = null;
                    }
                }
            }*/

            return iconData;
        }

        /*ivate static string GetSystemEntityIconPath(string objectTypeCode, bool isCustomEntity, string crmHostUrl)
        {
            //default system icon
            string defaultIconPath = isCustomEntity ? "/_imgs/ico_16_customEntity.gif" : "/_imgs/ico_16_systemEntity.gif";
            string iconUrl = string.Empty;
            if (!string.IsNullOrEmpty(objectTypeCode))
            {
                //get system entity icons
                string iconPath = "/_imgs/ico_16_" + objectTypeCode + ".gif";

                //not all system entities use GIF format
                iconUrl = crmHostUrl + iconPath;
                if (!UrlExists(iconUrl))
                {
                    iconPath = "/_imgs/ico_16_" + objectTypeCode + ".png";
                    iconUrl = crmHostUrl + iconPath;
                }
            }

            if (!UrlExists(iconUrl))
            {
                iconUrl = crmHostUrl + defaultIconPath;
            }

            return iconUrl;
        }

        private static bool UrlExists(string url)
        {
            bool result = false;

            var webRequest = WebRequest.Create(url);
            webRequest.Timeout = 20000; // miliseconds
            webRequest.Method = "HEAD";

            HttpWebResponse response = null;

            try
            {
                response = (HttpWebResponse)webRequest.GetResponse();
                result = true;
            }
            catch (WebException) { }
            finally
            {
                response?.Close();
            }

            return result;
        }*/
    }
}
