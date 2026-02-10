using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.WorkflowTasks
{
    public class WriteStringToFileParams : TaskParamsBase
    {
        public string Folder { get; set; }
        public string Filename { get; set; }
        public Encoding Encoding { get; set; }
        public string Timestamp { get; set; }
        public string Content { get; set; }
        public WriteStringToFileParams()
        {

        }
        public override void LoadResults(IGlobalContext globalContext)
        {
            _bindings.SetValueIfBindingExists<string>("Content", globalContext, (globalContext, value) => Content =value);
        }
    }
    public class WriteStringToFile : BaseTaskWithParams<WriteStringToFileParams>
    {
        public WriteStringToFile(string name, IGlobalContext globalContext, BaseTask parent, WriteStringToFileParams taskParams, ILogger<WriteStringToFile> log) : base(name, globalContext, parent, taskParams, log)
        {

        }
        public WriteStringToFile(string name, IGlobalContext globalContext, WriteStringToFileParams taskParams, ILogger<WriteStringToFile> log) : base(name, globalContext, taskParams, log)
        {

        }
        public override async Task Execute()
        {
            _log?.LogDebug($"ExportCSVLocalFS, filename:{TaskParams?.Filename}, Timestamp:{TaskParams?.Timestamp}");
            if (string.IsNullOrWhiteSpace(TaskParams.Filename))
            {
                _log?.LogWarning("WriteStringToFile, no filename defined");
                return;
            }
            if (string.IsNullOrWhiteSpace(TaskParams.Content))
            {
                _log?.LogWarning("WriteStringToFile, no content to export");
                return;
            }
            string filename = TaskParams.Filename;
            if (!string.IsNullOrWhiteSpace(TaskParams.Timestamp))
            {
                filename=Path.GetFileNameWithoutExtension(filename);
                filename=$"{filename}_{DateTime.Now.ToString(TaskParams.Timestamp)}";
                filename=$"{filename}{Path.GetExtension(TaskParams.Filename)}";
            }
            filename = Path.Combine(TaskParams.Folder, filename);
            _log?.LogDebug("saving to filename:{filename}, content length:{CsvContentLength}, Encoding:{Encoding}, CodePage:{CodePage}", 
                filename, 
                TaskParams.Content.Length, 
                TaskParams.Encoding, 
                TaskParams.Encoding.CodePage);
            await File.WriteAllTextAsync(filename, TaskParams.Content, TaskParams.Encoding);
        }
    }
}