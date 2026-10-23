using System;

namespace Formula81.XrmToolBox.Shared.Xrm.Exceptions
{
    internal class ServiceClientNotAvailableException : Exception
    {
        private const string ServiceClientNotAvailableMessage = "Service Client is not available";

        public ServiceClientNotAvailableException() : base(ServiceClientNotAvailableMessage) { }
    }
}
