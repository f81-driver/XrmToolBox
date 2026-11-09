using System;
using System.Collections.Generic;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    public class PreviewRow
    {
        public Guid Id { get; }
        public string EntityLogicalName { get; }
        // Formatted values, in the order of PreviewResult.CellNames; the grid binds to Cells[index]
        public string[] Cells { get; }

        public PreviewRow(Guid id, string entityLogicalName, string[] cells)
        {
            Id = id;
            EntityLogicalName = entityLogicalName;
            Cells = cells;
        }
    }

    public class PreviewResult
    {
        public IReadOnlyList<string> CellNames { get; }
        public IReadOnlyList<PreviewRow> Rows { get; }
        public int PageNumber { get; }
        public bool MoreRecords { get; }
        public string PagingCookie { get; }
        // -1 when not known (aggregate or top queries)
        public int TotalRecordCount { get; }
        public bool TotalRecordCountLimitExceeded { get; }

        public PreviewResult(IReadOnlyList<string> cellNames, IReadOnlyList<PreviewRow> rows, int pageNumber, bool moreRecords, string pagingCookie,
            int totalRecordCount, bool totalRecordCountLimitExceeded)
        {
            CellNames = cellNames;
            Rows = rows;
            PageNumber = pageNumber;
            MoreRecords = moreRecords;
            PagingCookie = pagingCookie;
            TotalRecordCount = totalRecordCount;
            TotalRecordCountLimitExceeded = totalRecordCountLimitExceeded;
        }
    }
}
