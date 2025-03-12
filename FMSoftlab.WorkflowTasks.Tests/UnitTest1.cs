using Dapper;
using FMSoftlab.WorkflowTasks.Flows;
using FMSoftlab.WorkflowTasks.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using System.Data;
using System.IO.Compression;

namespace FMSoftlab.WorkflowTasks.Tests
{

    public class SqlResultTest
    {
        public int Id { get; set; }
    }
    public class WorkflowTests
    {
        private readonly ILogger<WorkflowTests> _logger;
        private readonly ILoggerFactory _loggerFactory;
        public WorkflowTests()
        {
            var logger = new LoggerConfiguration()
          .MinimumLevel.Verbose()
          .WriteTo.Console()
          .WriteTo.File(@"C:\NetPandektisOutput\WorkflowTests.log", rollingInterval: RollingInterval.Day)
          .CreateLogger();

            _loggerFactory = new SerilogLoggerFactory(logger);
            _logger = _loggerFactory.CreateLogger<WorkflowTests>();
        }

        [Fact]
        public async Task UseSqlScalarResultFromStepOneAsInputToStepTwo()
        {
            int value = 88888888;
            Workflow wf = new Workflow("test", _loggerFactory);
            wf.AddTask<ExecuteSQL, ExecuteSQLParams>("ExecSql1",
                new ExecuteSQLParams()
                {
                    CommandTimeout=10,
                    MultiRow=false,
                    Scalar=true,
                    ConnectionString=@"Server=(localdb)\MSSQLLocalDB;Integrated Security=true",
                    CommandType=CommandType.Text,
                    ExecutionParams=new Dictionary<string, object>() { { "Id", value } },
                    Sql="select @id as Id"
                });
            wf.AddTask<ExecuteSQLTyped<SqlResultTest>, ExecuteSQLParams>("ExecSql2",
                new ExecuteSQLParams()
                {
                    CommandTimeout=10,
                    ConnectionString=@"Server=(localdb)\MSSQLLocalDB;Integrated Security=true",
                    CommandType=CommandType.Text,
                    Sql="select @id as Id"
                }, [new InputBinding<int, object>("ExecutionParams", "ExecSql1", "Result", (globalContext, x) =>
                {
                    return new { Id = x };
                })]);
            wf.AddTask<ConsoleWrite, ConsoleWriteParams>("write1",
                new ConsoleWriteParams(),
                [new InputBinding<IEnumerable<SqlResultTest>, string>("Message", "ExecSql2", "Result", (globalContext, x) =>
                {
                    return x.First().Id.ToString();
                })]);
            await wf.Start();
            var res1 = wf.GetTaskVariable("ExecSql1", "Result");
            var res2 = wf.GetTaskVariable("write1", "Result");
            Assert.NotNull(res1);
            Assert.NotNull(res2);
            Assert.Equal(value.ToString(), res1.ToString());
        }

        [Fact]
        public async Task ConnectToDbReturnValue()
        {
            Workflow wf = new Workflow("test", _loggerFactory);
            wf.AddTask<ExecuteSQLTyped<SqlResultTest>, ExecuteSQLParams>("ExecSql",
                new ExecuteSQLParams()
                {
                    CommandTimeout=10,
                    MultiRow=true,
                    ConnectionString=@"Server=(localdb)\MSSQLLocalDB;Integrated Security=true",
                    CommandType=CommandType.Text,
                    ExecutionParams=new Dictionary<string, object>() { { "Id", 88888888 } },
                    Sql="select @id as Id"
                });
            wf.AddTask<ExecuteSQL, ExecuteSQLParams>("ExecSql2",
                new ExecuteSQLParams()
                {
                    CommandTimeout=10,
                    MultiRow=false,
                    Scalar=true,
                    ConnectionString=@"Server=(localdb)\MSSQLLocalDB;Integrated Security=true",
                    CommandType=CommandType.Text,
                    ExecutionParams=new Dictionary<string, object>() { { "Id", 9999 } },
                    Sql="select @id as Id"
                });
            wf.AddTask<ConsoleWrite, ConsoleWriteParams>("write1",
                new ConsoleWriteParams(),
                [new InputBinding<IEnumerable<SqlResultTest>, string>("Message", "ExecSql", "Result", (globalContext, x) =>
                {
                    return x.First().Id.ToString();
                })]);
            wf.AddTask<ConsoleWrite, ConsoleWriteParams>("write2",
                new ConsoleWriteParams(),
                [new InputBinding<object, string>("Message", "ExecSql2", "Result", (globalContext, x) =>
                {
                    return x?.ToString()??string.Empty;
                })]);
            await wf.Start();
            var res1 = wf.GetTaskVariable("write1", "Result");
            var res2 = wf.GetTaskVariable("write2", "Result");
            Assert.NotNull(res1);
            Assert.NotNull(res2);
            Assert.Equal("88888888", res1.ToString());
            Assert.Equal("9999", res2.ToString());
        }

        [Fact]
        public async Task ExportExcel()
        {
            ILogger<ExportExcelFlow<SqlResultTest>> log = _loggerFactory.CreateLogger<ExportExcelFlow<SqlResultTest>>();
            IExportExcelFlowParams @params = new ExportExcelFlowParams()
            {
                Name="ExelExportFlow",
                ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true",
                LongRunningTimeout=100,
                ShortRunningTimeout=10,
                ExportFolder="Files",
                Filename="result.xlsx",
                StagingSqlCommandType=CommandType.Text,
                ExportSql= "select 1 as id union select 2 union select 3 as Id",
                ExportSqlCommandType=CommandType.Text,
                DataRoot="data",
                Template=@"Files\test.xlsx"
            };
            ExportExcelFlow<SqlResultTest> excport = new ExportExcelFlow<SqlResultTest>(@params, _loggerFactory, log);
            await excport.Execute();
        }

        [Fact]
        public async Task ExportExcelDatareader()
        {
            ILogger<ExportExcelFlow<SqlResultTest>> log = _loggerFactory.CreateLogger<ExportExcelFlow<SqlResultTest>>();

            Workflow wf = new Workflow("test", _loggerFactory);
            wf.AddTask<TransactionManager, TransactionManagerParams>("TransactionManager", new TransactionManagerParams()
            {
                ConnectionString=@"Server=(localdb)\MSSQLLocalDB;Integrated Security=true"
            });
            wf.AddTask<ExecuteSQLReader, ExecuteSQLParams>("ExecSql",
                new ExecuteSQLParams()
                {
                    CommandTimeout=10,
                    MultiRow=true,
                    CommandType=CommandType.Text,
                    ExecutionParams=new DynamicParameters(new { Id = 88888888 }),
                    Sql="select @id as Id"
                }, [new ResultBinding("TransactionManager", "TransactionManager")]);
            wf.AddTask<RenderExcelTemplate, RenderExcelTemplateParams>("render",
                new RenderExcelTemplateParams()
                {
                    DataRoot="data",
                    TemplateContent=System.IO.File.ReadAllBytes(@"Files\test.xlsx"),
                },
                [new ResultBinding("Datareader", "ExecSql")]);
            wf.AddTask<WriteBytesToFile, WriteBytesToFileParams>("WriteFileToDisk",
                new WriteBytesToFileParams()
                {
                    Filename="DataReaderExcel.xlsx",
                    Folder=@"c:\temp",
                    Timestamp="yyyyMMdd HHmmss"
                }, [new ResultBinding("FileContent", "render")]);
            wf.AddTask<TransactionCommit, CompleteTransactionParams>("TransactionCommit", new CompleteTransactionParams("TransactionManager"));
            await wf.Start();
        }

        [Fact]
        public async Task CopyFolder()
        {
            ILogger<Workflow> log = _loggerFactory.CreateLogger<Workflow>();
            Workflow wf = new Workflow("test", _loggerFactory);
            wf.AddTask<CopyFolder, CopyFolderParams>("CopyFolder",
                new CopyFolderParams()
                {
                    SourceFolder=@"C:\NetPandektisOutput",
                    DestinationFolder=@"c:\temp\destination\build_20230205_093753"
                });
            await wf.Start();
        }

        [Fact]
        public async Task ZipFolder()
        {
            ILogger<Workflow> log = _loggerFactory.CreateLogger<Workflow>();
            Workflow wf = new Workflow("test", _loggerFactory);
            wf.AddTask<FileZipper, FileZipperParams>("ZipFolder",
                new FileZipperParams()
                {
                    CompressionLevel=CompressionLevel.Optimal,
                    SourceDirectory=@"C:\temp\build_20230205_091726",
                    DestinationZipFile=@"c:\temp\destination\build_20230205_091726.zip",
                    IncludeBaseDirectory=true

                });
            await wf.Start();
        }
    }
}