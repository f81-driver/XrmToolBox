using Formula81.XrmToolBox.Shared.Core.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    /// <summary>
    /// A condition of the top level filter, edited directly on its FetchXML element so anything else on it is kept
    /// </summary>
    public class ViewCondition : ObservableObject
    {
        private const string AttributeAttributeName = "attribute";
        private const string OperatorAttributeName = "operator";
        private const string ValueAttributeName = "value";
        private const string ValueElementName = "value";
        private const string EntityNameAttributeName = "entityname";

        private static readonly char[] ValueSeparators = new[] { ',', ';', '\n', '\r' };

        public event EventHandler Changed;

        private readonly Func<string, ViewAttribute> _resolveAttribute;

        public XElement Element { get; }

        public string AttributeName
        {
            get => (string)Element.Attribute(AttributeAttributeName);
            set
            {
                // A combo box clears its selection when the value is not in its items, which must not wipe the condition
                if (string.IsNullOrEmpty(value) || value == AttributeName)
                {
                    return;
                }
                Element.SetAttributeValue(AttributeAttributeName, value);
                // The column is picked from the view table, so a condition on a related table becomes one on the view table
                Element.SetAttributeValue(EntityNameAttributeName, null);
                var previousOptions = Attribute?.HasOptions ?? false;
                Attribute = _resolveAttribute(value);
                // Checked against the operators of the column type itself: Operators also lists the current one to keep it visible
                var typeOperators = ConditionOperator.GetOperators(Attribute?.Kind ?? AttributeKind.Other);
                if (!typeOperators.Any(co => co.Value == Operator))
                {
                    SetOperator(typeOperators.FirstOrDefault()?.Value ?? ConditionOperator.Default.Value);
                    // A value for the old operator and column (such as %x% for like) means nothing for the new ones
                    if (ValueKind != ConditionValueKind.None)
                    {
                        SetValues(Enumerable.Empty<string>());
                    }
                }
                else if (previousOptions || (Attribute?.HasOptions ?? false))
                {
                    // Choice values of one column mean nothing for another
                    SetValues(Enumerable.Empty<string>());
                }
                NotifyPropertyChanged(nameof(AttributeName));
                NotifyPropertyChanged(nameof(EntityName));
                OnChanged();
            }
        }

        private ViewAttribute _attribute;
        public ViewAttribute Attribute
        {
            get => _attribute;
            private set
            {
                SetValue(nameof(Attribute), value, ref _attribute);
                NotifyPropertyChanged(nameof(Operators));
                NotifyPropertyChanged(nameof(Options));
                NotifyEditorChanged();
            }
        }

        public IReadOnlyList<ConditionOperator> Operators
        {
            get
            {
                var operators = ConditionOperator.GetOperators(Attribute?.Kind ?? AttributeKind.Other);
                var current = ConditionOperator.Get(Operator);
                // Keep an operator that is not offered for this type, so it is shown and kept as it is
                return operators.Any(co => co.Value == current.Value) ? operators : operators.Concat(new[] { current }).ToList();
            }
        }

        public string Operator
        {
            get => (string)Element.Attribute(OperatorAttributeName);
            set
            {
                if (string.IsNullOrEmpty(value) || value == Operator)
                {
                    return;
                }
                SetOperator(value);
                OnChanged();
            }
        }

        public ConditionValueKind ValueKind
        {
            get
            {
                var conditionOperator = ConditionOperator.Get(Operator);
                // An unknown operator with value elements (between, under, ...) takes several values
                return !conditionOperator.IsKnown && Element.Elements(ValueElementName).Any() ? ConditionValueKind.Multiple
                    : conditionOperator.ValueKind;
            }
        }

        public string Value
        {
            get
            {
                var values = Element.Elements(ValueElementName).Select(e => e.Value).ToList();
                return values.Any() ? string.Join(", ", values) : (string)Element.Attribute(ValueAttributeName);
            }
            set
            {
                if (value == Value)
                {
                    return;
                }
                SetValues(ValueKind == ConditionValueKind.Multiple ? (value ?? string.Empty).Split(ValueSeparators).Select(v => v.Trim()).Where(v => v.Length > 0)
                    : new[] { value ?? string.Empty });
                OnChanged();
            }
        }

        public IReadOnlyList<ViewOption> Options => Attribute?.Options ?? new ViewOption[0];

        public bool ShowChoiceValue => ValueKind == ConditionValueKind.Single && Options.Count > 0;
        public bool ShowTextValue => ValueKind == ConditionValueKind.Multiple || (ValueKind == ConditionValueKind.Single && Options.Count == 0);

        public string ValueToolTip
        {
            get
            {
                switch (ValueKind)
                {
                    case ConditionValueKind.Multiple:
                        return Options.Count > 0 ? "Values separated by commas: " + string.Join(", ", Options.Select(vo => vo.DisplayText)) : "Values separated by commas";
                    case ConditionValueKind.Single when Attribute?.Kind == AttributeKind.Date:
                        return "Date as yyyy-MM-dd, or a number of days or months for the X operators";
                    case ConditionValueKind.Single when Attribute?.Kind == AttributeKind.Lookup:
                        return "Id (GUID) of the record";
                    default:
                        return null;
                }
            }
        }

        // A condition on a column of a related table, set in the top level filter
        public string EntityName => (string)Element.Attribute(EntityNameAttributeName);
        public bool IsUnknownAttribute => Attribute == null;
        public string RawAttributeName => string.IsNullOrEmpty(EntityName) ? AttributeName : $"{EntityName}.{AttributeName}";

        public ViewCondition(XElement element, Func<string, ViewAttribute> resolveAttribute)
        {
            Element = element;
            _resolveAttribute = resolveAttribute;
            _attribute = string.IsNullOrEmpty(EntityName) ? resolveAttribute(AttributeName) : null;
        }

        private void SetOperator(string value)
        {
            var previousKind = ValueKind;
            Element.SetAttributeValue(OperatorAttributeName, value);
            var kind = ValueKind;
            if (kind == ConditionValueKind.None)
            {
                SetValues(Enumerable.Empty<string>());
            }
            else if (kind != previousKind)
            {
                // Keep the value(s) when switching between one and several values
                var values = Element.Elements(ValueElementName).Select(e => e.Value).ToList();
                var singleValue = (string)Element.Attribute(ValueAttributeName);
                SetValues(kind == ConditionValueKind.Multiple ? (singleValue != null ? new[] { singleValue } : values.ToArray())
                    : new[] { values.FirstOrDefault() ?? singleValue ?? string.Empty });
            }
            NotifyPropertyChanged(nameof(Operator));
            NotifyEditorChanged();
        }

        private void SetValues(IEnumerable<string> values)
        {
            // Before the value elements are removed, they decide the kind of an unknown operator
            var valueKind = ValueKind;
            var valueList = values.ToList();
            Element.SetAttributeValue(ValueAttributeName, null);
            Element.Elements(ValueElementName).Remove();
            if (valueKind == ConditionValueKind.Multiple)
            {
                Element.Add(valueList.Where(v => !string.IsNullOrEmpty(v)).Select(v => new XElement(ValueElementName, v)));
            }
            else if (valueKind == ConditionValueKind.Single)
            {
                Element.SetAttributeValue(ValueAttributeName, valueList.FirstOrDefault() ?? string.Empty);
            }
            NotifyPropertyChanged(nameof(Value));
        }

        private void NotifyEditorChanged()
        {
            NotifyPropertyChanged(nameof(ValueKind));
            NotifyPropertyChanged(nameof(ShowChoiceValue));
            NotifyPropertyChanged(nameof(ShowTextValue));
            NotifyPropertyChanged(nameof(ValueToolTip));
            NotifyPropertyChanged(nameof(IsUnknownAttribute));
            NotifyPropertyChanged(nameof(RawAttributeName));
        }

        private void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
