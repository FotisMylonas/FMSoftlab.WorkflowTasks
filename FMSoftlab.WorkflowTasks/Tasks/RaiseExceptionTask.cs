using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.WorkflowTasks.Tasks
{
    public class RaiseExceptionTaskParams : TaskParamsBase
    {
        public string ExceptionMessage { get; set; }

        public RaiseExceptionTaskParams()
        {

        }
        public override void LoadResults(IGlobalContext globalContext)
        {

        }
    }
    public class RaiseExceptionTask : BaseTaskWithParams<RaiseExceptionTaskParams>
    {
        public RaiseExceptionTask(string name, IGlobalContext globalContext, RaiseExceptionTaskParams taskParams, ILogger log) : base(name, globalContext, taskParams, log)
        {

        }

        public RaiseExceptionTask(string name, IGlobalContext globalContext, BaseTask parent, RaiseExceptionTaskParams taskParams, ILogger log) : base(name, globalContext, parent, taskParams, log)
        {

        }
        public override Task Execute()
        {
            string exceptionMessage = string.Empty;
            if (TaskParams is not null)
            {
                exceptionMessage= TaskParams.ExceptionMessage;
            }
            throw new Exception(exceptionMessage);
        }
    }
}
