using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.WorkflowTasks.Tasks
{
    public class WriteBytesToFileParams : TaskParamsBase
    {
        public string Filename { get; set; }
        public string Folder { get; set; }
        public string Timestamp { get; set; }
        public byte[] FileContent { get; set; }
        public WriteBytesToFileParams(IEnumerable<InputBinding> bindings) : base(bindings) { }
        public WriteBytesToFileParams() : base() { }
        public WriteBytesToFileParams(InputBinding binding) : base(binding) { }

        public override void LoadResults(IGlobalContext globalContext)
        {
            _bindings.SetValueIfBindingExists<byte[]>("FileContent", globalContext, (globalContext, value) => FileContent=value);
        }
    }
    public class WriteBytesToFile : BaseTaskWithParams<WriteBytesToFileParams>
    {
        public WriteBytesToFile(string name, IGlobalContext globalContext, BaseTask parent, WriteBytesToFileParams settings, ILogger<WriteBytesToFile> log) : base(name, globalContext, parent, settings, log)
        {

        }

        public override async Task Execute()
        {
            _log?.LogDebug($"WriteBytesToFile, Filename:{TaskParams.Filename}, FileContent:{TaskParams?.FileContent?.Length}");
            if (TaskParams?.FileContent is null || TaskParams?.FileContent?.Length<=0)
            {
                _log?.LogWarning("WriteBytesToFile, no content, exiting");
                return;
            }
            if (string.IsNullOrWhiteSpace(TaskParams.Filename))
            {
                _log?.LogWarning("WriteBytesToFile, empty filename, exiting");
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
            _log?.LogDebug($"Will write bytes to file:{filename}");
            try
            {
                await File.WriteAllBytesAsync(filename, TaskParams.FileContent);
                _log?.LogInformation($"Wrote {TaskParams.FileContent.Length} bytes to file {filename}");
            }
            catch (Exception ex)
            {
                _log?.LogError($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                throw;
            }
        }
    }
}