using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.WorkflowTasks.Tasks
{
    public class ReadFileAsBytesParams : TaskParamsBase
    {
        public string Filename { get; set; }
        public ReadFileAsBytesParams()
        {

        }

        public override void LoadResults(IGlobalContext globalContext)
        {

        }
    }
    public class ReadFileAsBytes : BaseTaskWithParams<ReadFileAsBytesParams>
    {
        public ReadFileAsBytes(string name, IGlobalContext globalContext, BaseTask parent, ReadFileAsBytesParams settings, ILogger log) : base(name, globalContext, parent, settings, log)
        {

        }

        public override async Task Execute()
        {
            byte[] fileBytes = new byte[0] { };
            if (!string.IsNullOrWhiteSpace(TaskParams.Filename))
            {
                fileBytes = await File.ReadAllBytesAsync(TaskParams.Filename);
            }
            GlobalContext.SetTaskVariable(Name, "Result", fileBytes);
        }
    }
}