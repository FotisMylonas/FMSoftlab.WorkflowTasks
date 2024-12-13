using FMSoftlab.WorkflowTasks;
using FMSoftlab.WorkflowTasks.Tasks;
using Microsoft.Extensions.Logging;
using System.Data;

namespace FMSoftlab.WorkflowTasks.Flows
{
    public interface IExportExcelFlowParams
    {
        int LongRunningTimeout { get; set; }
        int ShortRunningTimeout { get; set; }
        string Name { get; set; }
        string ConnectionString { get; set; }
        string StagingSql { get; set; }
        CommandType StagingSqlCommandType { get; set; }
        string ExportSql { get; set; }
        CommandType ExportSqlCommandType { get; set; }
        string ExportFolder { get; set; }
        string Filename { get; set; }
        string Template { get; set; }
        string TimeStamp { get; set; }
        string DataRoot { get; set; }
    }

    public class ExportExcelFlowParams : IExportExcelFlowParams
    {
        public int LongRunningTimeout { get; set; }
        public int ShortRunningTimeout { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string StagingSql { get; set; }
        public CommandType StagingSqlCommandType { get; set; }
        public string ExportSql { get; set; }
        public CommandType ExportSqlCommandType { get; set; }
        public string Template { get; set; }
        public string ExportFolder { get; set; }
        public string Filename { get; set; }
        public string TimeStamp { get; set; }
        public string DataRoot { get; set; }
        public ExportExcelFlowParams() : this(
            string.Empty,
            String.Empty,
            0,
            0,
            string.Empty,
            CommandType.StoredProcedure,
            string.Empty,
            CommandType.StoredProcedure,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty)
        { }
        public ExportExcelFlowParams(
            string name,
            string connectionString,
            int longRunningTimeout,
            int shortRunningTimeout,
            string stagingSql,
            CommandType stagingSqlCommandType,
            string exportSql,
            CommandType exportSqlCommandType,
            string template,
            string dataRoot,
            string exportFolder,
            string filename,
            string timeStamp)
        {
            Name = name;
            ConnectionString = connectionString;
            LongRunningTimeout = longRunningTimeout;
            ShortRunningTimeout = shortRunningTimeout;
            StagingSql = stagingSql;
            StagingSqlCommandType=stagingSqlCommandType;
            ExportSql = exportSql;
            ExportSqlCommandType=exportSqlCommandType;
            ExportFolder =exportFolder;
            Filename=filename;
            TimeStamp=timeStamp;
            Template=template;
            DataRoot=dataRoot;
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

        private void AddDataStaging(Workflow wf)
        {
            ExecuteSQLParams p1 = new ExecuteSQLParams()
            {
                CommandTimeout = _exportExcelFlowParams.LongRunningTimeout,
                CommandType = _exportExcelFlowParams.StagingSqlCommandType,
                ConnectionString = _exportExcelFlowParams.ConnectionString,
                Sql = _exportExcelFlowParams.StagingSql
            };
            wf.AddTask<ExecuteSQL, ExecuteSQLParams>("StageReport", p1);
        }

        private void AddDataExport(Workflow wf, int commandTimeout)
        {
            ExecuteSQLParams p2 = new ExecuteSQLParams()
            {
                CommandTimeout = commandTimeout,
                CommandType = _exportExcelFlowParams.ExportSqlCommandType,
                ConnectionString = _exportExcelFlowParams.ConnectionString,
                Sql = _exportExcelFlowParams.ExportSql
            };
            wf.AddTask<ExecuteSQLTyped<T>, ExecuteSQLParams>("ExportData", p2);
        }

        private void AddExcelRendering(Workflow wf)
        {
            ReadFileAsBytesParams p3 = new ReadFileAsBytesParams()
            {
                Filename = Path.GetFullPath(_exportExcelFlowParams.Template)
            };
            wf.AddTask<ReadFileAsBytes, ReadFileAsBytesParams>("ReadTemplate", p3);
            RenderExcelTemplateParams p4 = new RenderExcelTemplateParams() { DataRoot=_exportExcelFlowParams.DataRoot };
            wf.AddTask<RenderExcelTemplate, RenderExcelTemplateParams>("RenderExcel", p4,
                [new InputBinding("RenderingData", "ExportData", "Result"),
                new InputBinding("TemplateContent", "ReadTemplate", "Result")]
            );
            WriteBytesToFileParams p5 = new WriteBytesToFileParams()
            {
                Filename = _exportExcelFlowParams.Filename,
                Timestamp = _exportExcelFlowParams.TimeStamp,
                Folder = _exportExcelFlowParams.ExportFolder
            };
            wf.AddTask<WriteBytesToFile, WriteBytesToFileParams>("writebytes", p5,
                [new InputBinding("FileContent", "RenderExcel", "Result")]);
        }
        public async Task Execute()
        {
            _log?.LogDebug($"JobName:{_exportExcelFlowParams.Name} in");
            try
            {
                Workflow wf = new Workflow(_exportExcelFlowParams.Name, _logfact);
                int commandTimeout = _exportExcelFlowParams.LongRunningTimeout;
                if (!string.IsNullOrWhiteSpace(_exportExcelFlowParams.StagingSql))
                {
                    AddDataStaging(wf);
                    commandTimeout=_exportExcelFlowParams.ShortRunningTimeout;
                }
                AddDataExport(wf, commandTimeout);
                AddExcelRendering(wf);
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