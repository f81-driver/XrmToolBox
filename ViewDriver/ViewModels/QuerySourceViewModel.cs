using Formula81.XrmToolBox.Tools.ViewDriver.Models;
using System.Collections.Generic;

namespace Formula81.XrmToolBox.Tools.ViewDriver.ViewModels
{
    internal class QuerySourceViewModel
    {
        private readonly List<QuerySource> _querySourceList;
        public IEnumerable<QuerySource> QuerySources { get => _querySourceList; }


        public QuerySourceViewModel()
        {
            _querySourceList = new List<QuerySource> { QuerySource.System };
        }
    }
}
