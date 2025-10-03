using System.ComponentModel;

namespace Formula81.XrmToolBox.Libraries.Core.Components
{
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetValue<T>(string propertyName, T value, ref T valueObject)
        {
            OnPropertyChanging(propertyName);
            valueObject = value;
            OnPropertyChanged(propertyName);
            NotifyPropertyChanged(propertyName);
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanging(string propertyName) { }
        protected virtual void OnPropertyChanged(string propertyName) { }
    }
}
