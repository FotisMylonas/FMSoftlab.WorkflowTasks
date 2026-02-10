using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.WorkflowTasks.Tasks
{
    public class Workflow : BaseTask
    {
        public Workflow(string name, IGlobalContext globalContext) : base(
            name,
            globalContext,
            globalContext.LoggerFactory.CreateLogger<Workflow>())
        {

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
        public bool HasFailures => GlobalContext.HasFailures;
        public bool AllResultsSuccessful => GlobalContext.AllResultsSuccessful;
        public StepProcessResult MostRecentFailure => GlobalContext.MostRecentFailure;

    }
}