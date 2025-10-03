using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using System.Windows;
using System.Windows.Controls;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Controls
{
    public class EntityAuditValueTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultTemplate { get; set; }
        public DataTemplate EntityAuditRecordTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is EntityAuditValue value
                && value.DisplayValue is EntityLookupValue)
            {
                return EntityAuditRecordTemplate;
            }
            return DefaultTemplate;
        }
    }

    public partial class EntityAuditValueControl : UserControl
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(EntityAuditValue), typeof(EntityAuditValueControl));

        public EntityAuditValue Value { get => (EntityAuditValue)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

        public EntityAuditValueControl()
        {
            InitializeComponent();
        }
    }
}
