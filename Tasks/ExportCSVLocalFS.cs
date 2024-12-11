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
    public class ExportCSVLocalFSParams : TaskParamsBase
    {
        public string Folder { get; set; }
        public string Filename { get; set; }
        public Encoding Encoding { get; set; }
        public string Timestamp { get; set; }
        public string CsvContent { get; set; }
        public ExportCSVLocalFSParams()
        {

        }
        public override void LoadResults(IGlobalContext globalContext)
        {
            _bindings.SetValueIfBindingExists<string>("CsvContent", globalContext, (value) => CsvContent =value);
        }
    }
    public class ExportCSVLocalFS : BaseTaskWithParams<ExportCSVLocalFSParams>
    {
        public ExportCSVLocalFS(string name, IGlobalContext globalContext, BaseTask parent, ExportCSVLocalFSParams taskParams, ILogger<ExportCSVLocalFS> log) : base(name, globalContext, parent, taskParams, log)
        {

        }
        public ExportCSVLocalFS(string name, IGlobalContext globalContext, ExportCSVLocalFSParams taskParams, ILogger<ExportCSVLocalFS> log) : base(name, globalContext, taskParams, log)
        {

        }
        public override async Task Execute()
        {
            _log?.LogDebug($"ExportCSVLocalFS, filename:{TaskParams?.Filename}, Timestamp:{TaskParams?.Timestamp}");
            if (string.IsNullOrWhiteSpace(TaskParams.Filename))
            {
                _log?.LogWarning("ExportCSVLocalFS, no filename defined");
                return;
            }
            if (string.IsNullOrWhiteSpace(TaskParams.CsvContent))
            {
                _log?.LogWarning("ExportCSVLocalFS, no content to export");
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
            _log?.LogDebug($"saving to filename:{filename}, content length:{TaskParams.CsvContent.Length}, Encoding:{TaskParams.Encoding}");
            await File.WriteAllTextAsync(filename, TaskParams.CsvContent, TaskParams.Encoding);
        }
    }
}