using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.WorkflowTasks.Tasks
{
    public class QueryParamsParams : TaskParamsBase
    {
        public QueryParamsParams() : base()
        {

        }
        public Action<IDictionary<string, object>> SetParams { get; set; }

        public override void LoadResults(IGlobalContext globalContext)
        {

        }
    }
    public class QueryParams : BaseTaskWithParams<QueryParamsParams>
    {
        public QueryParams(string name, IGlobalContext globalContext, QueryParamsParams settings, ILogger log) : base(name, globalContext, settings, log)
        {
        }

        public QueryParams(string name, IGlobalContext globalContext, BaseTask parent, QueryParamsParams settings, ILogger log) : base(name, globalContext, parent, settings, log)
        {
        }

        public override Task Execute()
        {
            IDictionary<string, object> parameters = new Dictionary<string, object>();
            TaskParams.SetParams(parameters);
            GlobalContext.SetTaskVariable(Name, "QueryParameters", parameters);
            return Task.CompletedTask;
        }
    }
}
