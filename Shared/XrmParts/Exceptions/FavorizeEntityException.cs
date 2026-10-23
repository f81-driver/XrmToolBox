using System;

namespace Formula81.XrmToolBox.Shared.XrmParts.Exceptions
{
    public class FavorizeEntityException : Exception
    {
        private const string FavorizeEntityExceptionMessage = "Failed to favorize entity";
        public FavorizeEntityException(Exception exception) : base(FavorizeEntityExceptionMessage, exception)
        { }
    }
}
