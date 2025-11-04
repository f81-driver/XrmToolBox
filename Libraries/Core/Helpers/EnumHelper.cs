using System;
using System.Linq;

namespace Formula81.XrmToolBox.Libraries.Core.Helpers
{
    public static class EnumHelper
    {

        public static string GetEnumAttributeProperty<E, CA>(E enumValue, Func<CA, string> attributeSelector)
            where E : Enum
            where CA : Attribute
        {
            var memberInfo = typeof(E).GetMember(enumValue.ToString()).FirstOrDefault();
            var customerAttribute = memberInfo?.GetCustomAttributes(typeof(CA), false)?.FirstOrDefault();
            return customerAttribute is CA ca ? attributeSelector(ca) : enumValue.ToString();
        }
    }
}
