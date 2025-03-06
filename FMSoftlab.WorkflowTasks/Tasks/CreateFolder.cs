using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.WorkflowTasks.Tasks
{
    public class CreateFolderParams : TaskParamsBase
    {
        public string Folder { get; set; }
        public CreateFolderParams()
        {
        }
        public override void LoadResults(IGlobalContext globalContext)
        {
            _bindings.SetValueIfBindingExists<string>("Folder", globalContext, (globalContext, value) => Folder = value);
        }
    }
    public class CreateFolder : BaseTaskWithParams<CreateFolderParams>
    {
        public CreateFolder(string name, IGlobalContext globalContext, CreateFolderParams taskParams, ILogger log) : base(name, globalContext, taskParams, log)
        {
        }

        public CreateFolder(string name, IGlobalContext globalContext, BaseTask parent, CreateFolderParams taskParams, ILogger log) : base(name, globalContext, parent, taskParams, log)
        {

        }

        public override Task Execute()
        {
            if (TaskParams is null)
            {
                _log?.LogWarning("CreateFolder, no task params defined");
                return Task.CompletedTask;
            }
            if (string.IsNullOrWhiteSpace(TaskParams.Folder))
            {
                _log?.LogWarning("CreateFolder, no folder defined");
                return Task.CompletedTask;
            }
            if (!Directory.Exists(TaskParams.Folder))
            {
                Directory.CreateDirectory(TaskParams.Folder);
            }
            return Task.CompletedTask;
        }
    }
}