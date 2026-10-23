using System;

namespace Formula81.XrmToolBox.Shared.Parts.Components
{
    internal interface IDialogService
    {
        void ShowError(Exception exception, string heading);
    }
}
