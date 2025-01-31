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
    }
}