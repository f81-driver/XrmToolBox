using System;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Exceptions
{
    internal class AuditRecordsLimitException : Exception
    {
        private const string AuditRecordLimitMessage = "Amount of Audit Records is limited to {0}";

        private readonly int _limit;

        public AuditRecordsLimitException(int limit) 
            : base(string.Format(AuditRecordLimitMessage, limit))
        { 
            _limit = limit;
        }
    }
}
