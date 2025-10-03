using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Formula81.XrmToolBox.Libraries.Parts.Components
{
    public class SelectableEnumItem
    {
        public bool IsSelected { get; set; }
        public int EnumValue { get; }
        public string DisplayName { get; }

        public SelectableEnumItem(int enumValue, string displayName)
        {
            IsSelected = false;
            EnumValue = enumValue;
            DisplayName = displayName;
        }

        public static IEnumerable<SelectableEnumItem> ConvertEnum<E, A>(Func<A, string> displayNameSelector)
            where E : Enum
            where A : Attribute
        {
            var enumType = typeof(E);
            return Enum.GetValues(enumType)
                .Cast<E>()
                .Select(e => new SelectableEnumItem(Convert.ToInt32(e), displayNameSelector(enumType.GetField(e.ToString())
                        .GetCustomAttribute<A>())))
                .ToList();
        }
    }
}
