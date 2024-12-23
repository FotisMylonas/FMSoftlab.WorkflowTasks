using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.WorkflowTasks
{
    public class Workflow : BaseTask
    {
        private readonly ILoggerFactory _logFactory;
        public Workflow(string name, ILoggerFactory logFactory) : base(name, new GlobalContext(logFactory), null, logFactory.CreateLogger<Workflow>())
        {
            _logFactory = logFactory;
        }
        public override async Task Execute()
        {
            await Task.CompletedTask;
        }
        public async Task Start()
        {
            WorkflowEngine engine = new WorkflowEngine();
            await engine.Execute(this);
        }
        public TExecutionTask AddTask<TExecutionTask, TTaskParams>(string name, TTaskParams taskParams) where TExecutionTask : BaseTask where TTaskParams : TaskParamsBase
        {
            TExecutionTask executionTask = (TExecutionTask)Activator.CreateInstance(typeof(TExecutionTask), name, GlobalContext, this, taskParams, _logFactory.CreateLogger<TExecutionTask>());
            Tasks.Add(executionTask);
            return executionTask;
        }
        public TExecutionTask AddTask<TExecutionTask, TTaskParams>(string name, TTaskParams taskParams, IEnumerable<InputBinding> bindings) where TExecutionTask : BaseTask where TTaskParams : TaskParamsBase
        {
            taskParams.LoadBindings(bindings);
            TExecutionTask executionTask = (TExecutionTask)Activator.CreateInstance(typeof(TExecutionTask), name, GlobalContext, this, taskParams, _logFactory.CreateLogger<TExecutionTask>());
            Tasks.Add(executionTask);
            return executionTask;
        }
        public ExecuteSQL AddTask(string name, ExecuteSQLParams taskParams, IEnumerable<InputBinding> bindings)
        {
            return AddTask<ExecuteSQL, ExecuteSQLParams>(name, taskParams, bindings);
        }
    }
}