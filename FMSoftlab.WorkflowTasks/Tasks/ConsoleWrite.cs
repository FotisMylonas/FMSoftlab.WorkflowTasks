using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.WorkflowTasks.Tasks
{
    public class ConsoleWriteParams : TaskParamsBase
    {
        public string Message { get; set; }
        public ConsoleWriteParams()
        {
            Message = string.Empty;
        }
        public override void LoadResults(IGlobalContext globalContext)
        {
            _bindings.SetValueIfBindingExists<string>("Message", globalContext, (value) => Message=value);
        }
    }
    public class ConsoleWrite : BaseTaskWithParams<ConsoleWriteParams>
    {
        public ConsoleWrite(string name, IGlobalContext globalContext, ConsoleWriteParams settings, ILogger log) : base(name, globalContext, settings, log)
        {

        }

        public ConsoleWrite(string name, IGlobalContext globalContext, BaseTask parent, ConsoleWriteParams settings, ILogger log) : base(name, globalContext, parent, settings, log)
        {

        }

        public override async Task Execute()
        {
            Console.WriteLine(TaskParams.Message);
            SetTaskResult(TaskParams.Message);
            await Task.CompletedTask;
        }
    }
}