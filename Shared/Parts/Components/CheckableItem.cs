using Formula81.XrmToolBox.Shared.Core.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Formula81.XrmToolBox.Shared.Parts.Components
{
    public class CheckableItem<V> : ObservableObject
    {
        private bool _isChecked;
        public bool IsChecked { get => _isChecked; set => SetValue(nameof(IsChecked), value, ref _isChecked); }

        public V Value { get; }
        public string DisplayName { get; }

        public CheckableItem(V value, string displayName)
        {
            IsChecked = false;
            Value = value;
            DisplayName = displayName;
        }

        public static IEnumerable<CheckableItem<E>> ConvertEnum<E, A>(Func<A, string> displayNameSelector)
            where E : Enum
            where A : Attribute
        {
            var enumType = typeof(E);
            return Enum.GetValues(enumType)
                .Cast<E>()
                .Select(e => new CheckableItem<E>(e, displayNameSelector(enumType.GetField(e.ToString())
                        .GetCustomAttribute<A>())))
                .ToList();
        }
    }
}
