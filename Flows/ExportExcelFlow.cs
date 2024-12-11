using FMSoftlab.WorkflowTasks;
using FMSoftlab.WorkflowTasks.Tasks;
using Microsoft.Extensions.Logging;
using System.Data;

namespace FMSoftlab.WorkflowTasks.Flows
{

    public interface IExportExcelFlowParams
    {
        public int LongRunningTimeout { get; set; }
        public int ShortRunningTimeout { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string StagingSql { get; set; }
        public string ExportSql { get; set; }
        public string ExportFolder { get; set; }
        public string Template { get; set; }
        public string Filename { get; set; }
        public string TimeStamp { get; set; }
    }

    public class ExportExcelFlowParams : IExportExcelFlowParams
    {
        public int LongRunningTimeout { get; set; }
        public int ShortRunningTimeout { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string StagingSql { get; set; }
        public string ExportSql { get; set; }
        public string Template { get; set; }
        public string ExportFolder { get; set; }
        public string Filename { get; set; }
        public string TimeStamp { get; set; }
        public ExportExcelFlowParams(
            string name,
            string connectionString,
            int longRunningTimeout,
            int shortRunningTimeout,
            string stagingSql,
            string exportSql,
            string template,
            string exportFolder,
            string filename,
            string timeStamp)
        {
            Name = name;
            ConnectionString = connectionString;
            LongRunningTimeout = longRunningTimeout;
            ShortRunningTimeout = shortRunningTimeout;
            StagingSql = stagingSql;
            ExportSql = exportSql;
            ExportFolder=exportFolder;
            Filename=filename;
            TimeStamp=timeStamp;
            Template=template;
        }
    }

    public class ExportExcelFlow<T>
    {
        private readonly ILogger<ExportExcelFlow<T>> _log;
        private readonly IExportExcelFlowParams _exportExcelFlowParams;
        private readonly ILoggerFactory _logfact;
        public ExportExcelFlow(IExportExcelFlowParams exportExcelFlowParams, ILoggerFactory logfact, ILogger<ExportExcelFlow<T>> log)
        {
            _exportExcelFlowParams = exportExcelFlowParams;
            _log = log;
            _logfact = logfact;
        }
        public async Task Execute()
        {
            _log?.LogDebug($"JobName:{_exportExcelFlowParams.Name} in");
            try
            {
                Workflow wf = new Workflow(_exportExcelFlowParams.Name, _logfact);
                ExecuteSQLParams p1 = new ExecuteSQLParams()
                {
                    CommandTimeout = _exportExcelFlowParams.LongRunningTimeout,
                    CommandType = CommandType.StoredProcedure,
                    ConnectionString = _exportExcelFlowParams.ConnectionString,
                    Sql = _exportExcelFlowParams.StagingSql
                };
                wf.AddTask<ExecuteSQL, ExecuteSQLParams>("StagePrenotationsReport", p1);
                ExecuteSQLParams p2 = new ExecuteSQLParams()
                {
                    CommandTimeout = _exportExcelFlowParams.ShortRunningTimeout,
                    CommandType = CommandType.StoredProcedure,
                    ConnectionString = _exportExcelFlowParams.ConnectionString,
                    Sql = _exportExcelFlowParams.ExportSql
                };
                wf.AddTask<ExecuteSQLTyped<T>, ExecuteSQLParams>("ExportData", p2);

                ReadFileAsBytesParams p3 = new ReadFileAsBytesParams()
                {
                    Filename = Path.GetFullPath(_exportExcelFlowParams.Template)
                };
                wf.AddTask<ReadFileAsBytes, ReadFileAsBytesParams>("ReadTemplate", p3);
                RenderExcelTemplateParams p4 = new RenderExcelTemplateParams();
                wf.AddTask<RenderExcelTemplate, RenderExcelTemplateParams>("RenderExcel", p4,
                    [new InputBinding("RenderingData", "ExportData", "Result"),
                    new InputBinding("TemplateContent", "ReadTemplate", "Result")]
                );
                WriteBytesToFileParams p5 = new WriteBytesToFileParams()
                {
                    Filename = Path.Combine(_exportExcelFlowParams.ExportFolder, _exportExcelFlowParams.Filename),
                    Timestamp = _exportExcelFlowParams.TimeStamp,
                    Folder = _exportExcelFlowParams.ExportFolder
                };
                wf.AddTask<WriteBytesToFile, WriteBytesToFileParams>("writebytes", p5,
                    [new InputBinding("FileContent", "RenderExcel", "Result")]);

                await wf.Start();
            }
            catch (Exception e)
            {
                _log?.LogError($"{e.Message}{Environment.NewLine}{e.StackTrace}");
                throw;
            }
            _log?.LogDebug($"JobName:{_exportExcelFlowParams.Name} out");
        }
    }
}