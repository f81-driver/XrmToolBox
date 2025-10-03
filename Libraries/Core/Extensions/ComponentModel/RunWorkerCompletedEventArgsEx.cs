using System.ComponentModel;

namespace Formula81.XrmToolBox.Libraries.Core.Extensions.ComponentModel
{
    public static class RunWorkerCompletedEventArgsEx
    {
        public static void ThrowIfError(this RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                throw e.Error;
            }
        }
    }
}
