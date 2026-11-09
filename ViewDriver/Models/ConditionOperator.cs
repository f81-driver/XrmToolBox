using System;
using System.Collections.Generic;
using System.Linq;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    public enum ConditionValueKind
    {
        None,
        Single,
        Multiple
    }

    /// <summary>
    /// A FetchXML condition operator with a friendly label
    /// </summary>
    public class ConditionOperator
    {
        public string Value { get; }
        public string Label { get; }
        public ConditionValueKind ValueKind { get; }
        // False for an operator this tool does not know, kept as it is
        public bool IsKnown { get; }

        public string ToolTip => Value;

        private ConditionOperator(string value, string label, ConditionValueKind valueKind, bool isKnown = true)
        {
            Value = value;
            Label = label;
            ValueKind = valueKind;
            IsKnown = isKnown;
        }

        private static readonly ConditionOperator Equal = new ConditionOperator("eq", "Equals", ConditionValueKind.Single);
        private static readonly ConditionOperator NotEqual = new ConditionOperator("ne", "Does not equal", ConditionValueKind.Single);
        private static readonly ConditionOperator NotNull = new ConditionOperator("not-null", "Contains data", ConditionValueKind.None);
        private static readonly ConditionOperator Null = new ConditionOperator("null", "Does not contain data", ConditionValueKind.None);
        private static readonly ConditionOperator In = new ConditionOperator("in", "Is any of", ConditionValueKind.Multiple);
        private static readonly ConditionOperator NotIn = new ConditionOperator("not-in", "Is none of", ConditionValueKind.Multiple);
        private static readonly ConditionOperator Like = new ConditionOperator("like", "Like (% is wildcard)", ConditionValueKind.Single);
        private static readonly ConditionOperator NotLike = new ConditionOperator("not-like", "Not like (% is wildcard)", ConditionValueKind.Single);
        private static readonly ConditionOperator BeginsWith = new ConditionOperator("begins-with", "Begins with", ConditionValueKind.Single);
        private static readonly ConditionOperator NotBeginWith = new ConditionOperator("not-begin-with", "Does not begin with", ConditionValueKind.Single);
        private static readonly ConditionOperator EndsWith = new ConditionOperator("ends-with", "Ends with", ConditionValueKind.Single);
        private static readonly ConditionOperator NotEndWith = new ConditionOperator("not-end-with", "Does not end with", ConditionValueKind.Single);
        private static readonly ConditionOperator GreaterThan = new ConditionOperator("gt", "Greater than", ConditionValueKind.Single);
        private static readonly ConditionOperator GreaterEqual = new ConditionOperator("ge", "Greater than or equal to", ConditionValueKind.Single);
        private static readonly ConditionOperator LessThan = new ConditionOperator("lt", "Less than", ConditionValueKind.Single);
        private static readonly ConditionOperator LessEqual = new ConditionOperator("le", "Less than or equal to", ConditionValueKind.Single);
        private static readonly ConditionOperator Between = new ConditionOperator("between", "Between (two values)", ConditionValueKind.Multiple);
        private static readonly ConditionOperator NotBetween = new ConditionOperator("not-between", "Not between (two values)", ConditionValueKind.Multiple);
        private static readonly ConditionOperator On =new ConditionOperator("on", "On", ConditionValueKind.Single);
        private static readonly ConditionOperator OnOrAfter = new ConditionOperator("on-or-after", "On or after", ConditionValueKind.Single);
        private static readonly ConditionOperator OnOrBefore = new ConditionOperator("on-or-before", "On or before", ConditionValueKind.Single);
        private static readonly ConditionOperator Today = new ConditionOperator("today", "Today", ConditionValueKind.None);
        private static readonly ConditionOperator Yesterday = new ConditionOperator("yesterday", "Yesterday", ConditionValueKind.None);
        private static readonly ConditionOperator Tomorrow = new ConditionOperator("tomorrow", "Tomorrow", ConditionValueKind.None);
        private static readonly ConditionOperator ThisWeek = new ConditionOperator("this-week", "This week", ConditionValueKind.None);
        private static readonly ConditionOperator LastWeek = new ConditionOperator("last-week", "Last week", ConditionValueKind.None);
        private static readonly ConditionOperator ThisMonth = new ConditionOperator("this-month", "This month", ConditionValueKind.None);
        private static readonly ConditionOperator LastMonth = new ConditionOperator("last-month", "Last month", ConditionValueKind.None);
        private static readonly ConditionOperator ThisYear = new ConditionOperator("this-year", "This year", ConditionValueKind.None);
        private static readonly ConditionOperator LastYear = new ConditionOperator("last-year", "Last year", ConditionValueKind.None);
        private static readonly ConditionOperator LastXDays = new ConditionOperator("last-x-days", "Last X days", ConditionValueKind.Single);
        private static readonly ConditionOperator NextXDays = new ConditionOperator("next-x-days", "Next X days", ConditionValueKind.Single);
        private static readonly ConditionOperator LastXMonths = new ConditionOperator("last-x-months", "Last X months", ConditionValueKind.Single);
        private static readonly ConditionOperator NextXMonths = new ConditionOperator("next-x-months", "Next X months", ConditionValueKind.Single);
        private static readonly ConditionOperator OlderThanXDays = new ConditionOperator("olderthan-x-days", "Older than X days", ConditionValueKind.Single);
        private static readonly ConditionOperator OlderThanXMonths = new ConditionOperator("olderthan-x-months", "Older than X months", ConditionValueKind.Single);
        private static readonly ConditionOperator EqualUserId = new ConditionOperator("eq-userid", "Equals current user", ConditionValueKind.None);
        private static readonly ConditionOperator NotEqualUserId = new ConditionOperator("ne-userid", "Does not equal current user", ConditionValueKind.None);
        private static readonly ConditionOperator EqualUserTeams = new ConditionOperator("eq-useroruserteams", "Equals current user or their teams", ConditionValueKind.None);
        private static readonly ConditionOperator EqualBusinessId = new ConditionOperator("eq-businessid", "Equals current business unit", ConditionValueKind.None);
        private static readonly ConditionOperator ContainValues = new ConditionOperator("contain-values", "Contains values", ConditionValueKind.Multiple);
        private static readonly ConditionOperator NotContainValues = new ConditionOperator("not-contain-values", "Does not contain values", ConditionValueKind.Multiple);

        private static readonly IReadOnlyList<ConditionOperator> TextOperators = new[] { Equal, NotEqual, Like, NotLike, BeginsWith, NotBeginWith, EndsWith, NotEndWith, In, NotIn, NotNull, Null };
        private static readonly IReadOnlyList<ConditionOperator> NumberOperators = new[] { Equal, NotEqual, GreaterThan, GreaterEqual, LessThan, LessEqual, Between, NotBetween, In, NotIn, NotNull, Null };
        private static readonly IReadOnlyList<ConditionOperator> DateOperators = new[] { On, OnOrAfter, OnOrBefore, GreaterThan, LessThan, Between, NotBetween, Today, Yesterday, Tomorrow, ThisWeek, LastWeek, ThisMonth, LastMonth, ThisYear, LastYear,
            LastXDays, NextXDays, LastXMonths, NextXMonths, OlderThanXDays, OlderThanXMonths, NotNull, Null };
        private static readonly IReadOnlyList<ConditionOperator> LookupOperators = new[] { Equal, NotEqual, In, NotIn, EqualUserId, NotEqualUserId, EqualUserTeams, EqualBusinessId, NotNull, Null };
        private static readonly IReadOnlyList<ConditionOperator> ChoiceOperators = new[] { Equal, NotEqual, In, NotIn, NotNull, Null };
        private static readonly IReadOnlyList<ConditionOperator> MultiChoiceOperators = new[] { ContainValues, NotContainValues, Equal, NotNull, Null };
        private static readonly IReadOnlyList<ConditionOperator> BooleanOperators = new[] { Equal, NotEqual, NotNull, Null };
        private static readonly IReadOnlyList<ConditionOperator> IdentifierOperators = new[] { Equal, NotEqual, In, NotIn, NotNull, Null };
        private static readonly IReadOnlyList<ConditionOperator> OtherOperators = new[] { Equal, NotEqual, NotNull, Null };

        private static readonly Dictionary<string, ConditionOperator> AllOperators = new[] { TextOperators, NumberOperators, DateOperators, LookupOperators, ChoiceOperators, MultiChoiceOperators, BooleanOperators, IdentifierOperators }
            .SelectMany(co => co)
            .GroupBy(co => co.Value)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        public static ConditionOperator Default => NotNull;

        public static IReadOnlyList<ConditionOperator> GetOperators(AttributeKind attributeKind)
        {
            switch (attributeKind)
            {
                case AttributeKind.Text: return TextOperators;
                case AttributeKind.Number: return NumberOperators;
                case AttributeKind.Date: return DateOperators;
                case AttributeKind.Lookup: return LookupOperators;
                case AttributeKind.Choice: return ChoiceOperators;
                case AttributeKind.MultiChoice: return MultiChoiceOperators;
                case AttributeKind.Boolean: return BooleanOperators;
                case AttributeKind.Identifier: return IdentifierOperators;
                default: return OtherOperators;
            }
        }

        /// <summary>
        /// Every FetchXML operator is accepted; one that is not known here keeps its own name and is assumed to take one value
        /// </summary>
        public static ConditionOperator Get(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Default;
            }
            return AllOperators.TryGetValue(value, out var conditionOperator) ? conditionOperator
                : new ConditionOperator(value, value, ConditionValueKind.Single, false);
        }
    }
}
