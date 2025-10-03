using System;
using System.Collections.Generic;

namespace Formula81.XrmToolBox.Libraries.Xrm.Resources
{
    public static class XrmResourceRepository
    {
        private const string DefaultEntityIconUrl = "pack://application:,,,/Formula81.XrmToolBox.Libraries.Xrm;component/Resources/EntityIcons/ico_16_customEntity.gif";

        private static readonly Uri DefaultEntityIconUri = new Uri(DefaultEntityIconUrl);
        private static readonly HashSet<int> EntityIconObjectTypeCodeSet = new HashSet<int>{ 1, 2, 3, 4, 8, 9, 10, 50, 112, 123, 126, 127, 129,
            1010, 1011, 1013, 1022, 1036, 1055, 1056, 1084, 1085, 1088, 1089, 1090, 1091, 1145, 1146, 1147, 1148, 1149, 1150, 1151, 1152,
            2020, 2029, 3234, 4000, 4001, 4002, 4003, 4009, 4200, 4201, 4202, 4204, 4206, 4207, 4208, 4209, 4210, 4211, 4212, 4214 };

        public static Uri GetEntityIconUri(int? objectTypeCode)
        {
            return objectTypeCode.HasValue
                && EntityIconObjectTypeCodeSet.Contains(objectTypeCode.Value)
                    ? new Uri($"pack://application:,,,/Formula81.XrmToolBox.Libraries.Xrm;component/Resources/EntityIcons/ico_16_{objectTypeCode.Value}.gif")
                        : DefaultEntityIconUri;

        }
    }
}
