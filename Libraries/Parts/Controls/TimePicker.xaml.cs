using System;
using System.Windows;
using System.Windows.Controls;

namespace Formula81.XrmToolBox.Libraries.Parts.Controls
{
    public partial class TimePicker : UserControl
    {
        public static readonly DependencyProperty SelectedTimeProperty = DependencyProperty.Register("SelectedTime", typeof(DateTime), typeof(TimePicker), new PropertyMetadata(DateTime.Now, OnSelectedTimeChanged));
        public static readonly DependencyProperty HourProperty = DependencyProperty.Register("Hour", typeof(int), typeof(TimePicker), new PropertyMetadata(0, OnHourChanged));
        public static readonly DependencyProperty MinuteProperty = DependencyProperty.Register("Minute", typeof(int), typeof(TimePicker), new PropertyMetadata(0, OnMinuteChanged));
        public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register("IsDropDownOpen", typeof(bool), typeof(TimePicker), new PropertyMetadata(false));

        public DateTime SelectedTime { get { return (DateTime)GetValue(SelectedTimeProperty); } set { SetValue(SelectedTimeProperty, value); } }
        public int Hour { get { return (int)GetValue(HourProperty); } set { SetValue(HourProperty, value); } }
        public int Minute { get { return (int)GetValue(MinuteProperty); } set { SetValue(MinuteProperty, value); } }
        public bool IsDropDownOpen { get { return (bool)GetValue(IsDropDownOpenProperty); } set { SetValue(IsDropDownOpenProperty, value); } }

        // Temp values for cancel operation
        private DateTime _tempSelectedTime;

        public TimePicker()
        {
            InitializeComponent();
            UpdateTimeComponents();
        }

        // Property change handlers
        private static void OnSelectedTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TimePicker)d;
            control.UpdateTimeComponents();
        }

        private static void OnHourChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TimePicker)d;
            control.UpdateSelectedTimeFromComponents();
        }

        private static void OnMinuteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TimePicker)d;
            control.UpdateSelectedTimeFromComponents();
        }

        // Helper methods
        private void UpdateTimeComponents()
        {
            Hour = SelectedTime.Hour;
            Minute = SelectedTime.Minute;
        }

        private void UpdateSelectedTimeFromComponents()
        {
            DateTime newDateTime = new DateTime(SelectedTime.Year, SelectedTime.Month, SelectedTime.Day, Hour, Minute, 0);
            if (newDateTime != SelectedTime)
                SelectedTime = newDateTime;
        }

        // Event handlers
        private void HourUp_Click(object sender, RoutedEventArgs e)
        {
            Hour = (Hour + 1) % 24;
        }

        private void HourDown_Click(object sender, RoutedEventArgs e)
        {
            Hour = (Hour + 23) % 24;
        }

        private void MinuteUp_Click(object sender, RoutedEventArgs e)
        {
            Minute = (Minute + 1) % 60;
        }

        private void MinuteDown_Click(object sender, RoutedEventArgs e)
        {
            Minute = (Minute + 59) % 60;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            IsDropDownOpen = false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedTime = _tempSelectedTime;
            IsDropDownOpen = false;
        }
    }
}
