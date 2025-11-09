using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Formula81.XrmToolBox.Shared.Parts.Components
{
    public class CheckableEnumItem
    {
        public bool IsChecked { get; set; }
        public int EnumValue { get; }
        public string DisplayName { get; }

        public CheckableEnumItem(int enumValue, string displayName)
        {
            IsChecked = false;
            EnumValue = enumValue;
            DisplayName = displayName;
        }

        public static IEnumerable<CheckableEnumItem> ConvertEnum<E, A>(Func<A, string> displayNameSelector)
            where E : Enum
            where A : Attribute
        {
            var enumType = typeof(E);
            return Enum.GetValues(enumType)
                .Cast<E>()
                .Select(e => new CheckableEnumItem(Convert.ToInt32(e), displayNameSelector(enumType.GetField(e.ToString())
                        .GetCustomAttribute<A>())))
                .ToList();
        }
    }
}
