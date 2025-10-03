using Formula81.XrmToolBox.Libraries.Parts.Components;
using Formula81.XrmToolBox.Libraries.Parts.Input;
using Formula81.XrmToolBox.Libraries.Xrm;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Windows
{
    public partial class EntityAuditCritieriaWindow : Window
    {
        public IEnumerable<ConditionExpression> Criteria { get => GetCriteria(); set => SetCriteria(value); }

        public EntityAuditCritieriaWindow()
        {
            var auditOperationType = typeof(Audit_Operation);

            InitializeComponent();

            OperationListBox.ItemsSource = SelectableEnumItem.ConvertEnum<Audit_Operation, OptionSetMetadataAttribute>(osma => osma.Name);
            ActionListBox.ItemsSource = SelectableEnumItem.ConvertEnum<Audit_Action, OptionSetMetadataAttribute>(osma => osma.Name);

            SubmitButton.Command = new RelayCommand(ExecuteSubmit, CanExecuteSubmit);
            CancelButton.Command = new RelayCommand(ExecuteCancel, CanExecuteCancel);
        }

        private bool CanExecuteSubmit(object parameter)
        {
            return true;
        }

        private bool CanExecuteCancel(object parameter)
        {
            return true;
        }

        private void ExecuteSubmit(object parameter)
        {
            DialogResult = true;
            Close();
        }

        private void ExecuteCancel(object parameter)
        {
            DialogResult = false;
            Close();
        }

        private void ClearCriteria()
        {
            ChangedDateFromCheckBox.IsChecked = false;
            ChangedDateFromDatePicker.SelectedDate = null;
            ChangedDateFromTimePicker.Hour = DateTime.Now.AddHours(-1).Hour;
            ChangedDateFromTimePicker.Minute = DateTime.Now.Minute;
            ChangedDateToCheckBox.IsChecked = false;
            ChangedDateToDatePicker.SelectedDate = null;
            ChangedDateToTimePicker.Hour = DateTime.Now.Hour;
            ChangedDateToTimePicker.Minute = DateTime.Now.Minute;
            OperationCheckBox.IsChecked = false;
            foreach (var operationItem in OperationListBox.Items.OfType<SelectableEnumItem>())
            {
                operationItem.IsSelected = false;
            }
            ActionCheckBox.IsChecked = false;
            foreach (var actionItem in ActionListBox.Items.OfType<SelectableEnumItem>())
            {
                actionItem.IsSelected = false;
            }
        }

        private IEnumerable<ConditionExpression> GetCriteria()
        {
            if ((ChangedDateFromCheckBox.IsChecked ?? false)
                && ChangedDateFromDatePicker.SelectedDate.HasValue)
            {
                var changedDateFrom = DateTime.SpecifyKind(ChangedDateFromDatePicker.SelectedDate.Value
                    .AddTicks(ChangedDateFromTimePicker.SelectedTime.TimeOfDay.Ticks), DateTimeKind.Local).ToUniversalTime();
                yield return new ConditionExpression(Audit.ColumnNames.CreatedOn, ConditionOperator.GreaterEqual, changedDateFrom);
            }

            if ((ChangedDateToCheckBox.IsChecked ?? false)
                && ChangedDateToDatePicker.SelectedDate.HasValue)
            {
                var changedDateTo = DateTime.SpecifyKind(ChangedDateToDatePicker.SelectedDate.Value
                    .AddTicks(ChangedDateToTimePicker.SelectedTime.TimeOfDay.Ticks), DateTimeKind.Local).ToUniversalTime();
                yield return new ConditionExpression(Audit.ColumnNames.CreatedOn, ConditionOperator.LessEqual, changedDateTo);
            }

            if (OperationCheckBox.IsChecked ?? false)
            {
                var operations = OperationListBox.Items?.OfType<SelectableEnumItem>()
                    .Where(sei => sei.IsSelected);
                if (operations.Any())
                {
                    yield return new ConditionExpression(Audit.ColumnNames.Operation, ConditionOperator.In, operations.Select(o => o.EnumValue).ToArray());
                }
            }
            if (ActionCheckBox.IsChecked ?? false)
            {
                var actions = ActionListBox.Items.OfType<SelectableEnumItem>()
                    .Where(sei => sei.IsSelected);
                if (actions.Any())
                {
                    yield return new ConditionExpression(Audit.ColumnNames.Action, ConditionOperator.In, actions.Select(a => a.EnumValue).ToArray());
                }
            }
        }

        private void SetCriteria(IEnumerable<ConditionExpression> criteria)
        {
            ClearCriteria();
            if (criteria != null)
            {
                foreach (var condition in criteria)
                {
                    if (condition.AttributeName.Equals(Audit.ColumnNames.CreatedOn))
                    {
                        var changedDate = condition.Values.OfType<DateTime?>().FirstOrDefault();
                        if (changedDate.HasValue)
                        {
                            var changedDateValue = changedDate.Value.ToLocalTime();
                            switch (condition.Operator)
                            {
                                case ConditionOperator.GreaterEqual:
                                    ChangedDateFromCheckBox.IsChecked = true;
                                    ChangedDateFromDatePicker.SelectedDate = changedDateValue.Date;
                                    ChangedDateFromTimePicker.Hour = changedDateValue.Hour;
                                    ChangedDateFromTimePicker.Minute = changedDateValue.Minute;
                                    break;
                                case ConditionOperator.LessEqual:
                                    ChangedDateToCheckBox.IsChecked = true;
                                    ChangedDateToDatePicker.SelectedDate = changedDateValue.Date;
                                    ChangedDateToTimePicker.Hour = changedDateValue.Hour;
                                    ChangedDateToTimePicker.Minute = changedDateValue.Minute;
                                    break;
                            }
                        }
                    }
                    else if (condition.AttributeName.Equals(Audit.ColumnNames.Operation))
                    {
                        foreach (var operationValue in condition.Values.OfType<int>())
                        {
                            var operationItem = OperationListBox.Items.OfType<SelectableEnumItem>().FirstOrDefault(sei => sei.EnumValue == operationValue);
                            if (operationItem != null)
                            {
                                operationItem.IsSelected = true;
                            }
                        }
                    }
                    else if (condition.AttributeName.Equals(Audit.ColumnNames.Action))
                    {
                        foreach (var actionValue in condition.Values.OfType<int>())
                        {
                            var actionItem = ActionListBox.Items.OfType<SelectableEnumItem>().FirstOrDefault(sei => sei.EnumValue == actionValue);
                            if (actionItem != null)
                            {
                                actionItem.IsSelected = true;
                            }
                        }
                    }
                }
            }
        }
    }
}
