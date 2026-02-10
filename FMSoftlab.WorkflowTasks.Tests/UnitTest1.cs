using Dapper;
using FMSoftlab.WorkflowTasks.Flows;
using FMSoftlab.WorkflowTasks.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceProvider _serviceProvider;
        public WorkflowTests()
        {
            var logger = new LoggerConfiguration()
          .MinimumLevel.Verbose()
          .WriteTo.Console()
          .WriteTo.File(@"C:\NetPandektisOutput\WorkflowTests.log", rollingInterval: RollingInterval.Day)
          .CreateLogger();

            _loggerFactory = new SerilogLoggerFactory(logger);
            _logger = _loggerFactory.CreateLogger<WorkflowTests>();

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register logging
            services.AddSingleton(_loggerFactory);
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

            // Register HttpClient
            services.AddHttpClient<ReportServerClient>()
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromMinutes(5);
                });

            // Register your services
            services.AddTransient<ReportServerGetFile>();

            // Add any other dependencies your tests need
            HttpClientOptions options=new HttpClientOptions()
            {
                UserName="fotis",
                Password="12345678",
                Domain="eurodomain"
            };
            services.AddReportServerHttpClient(options);
        }

        [Fact]
        public async Task UseSqlScalarResultFromStepOneAsInputToStepTwo()
        {
            int value = 88888888;

            GlobalContext globalContext = new GlobalContext(_serviceProvider, _loggerFactory);
            Workflow wf = new Workflow("test", globalContext);
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
                }, [new ResultBinding<int, object>("ExecutionParams", "ExecSql1", (globalContext, x) =>
                {
                    return new { Id = x };
                })]);
            wf.AddTask<ConsoleWrite, ConsoleWriteParams>("write1",
                new ConsoleWriteParams(),
                [new ResultBinding<IEnumerable<SqlResultTest>, string>("Message", "ExecSql2", (globalContext, x) =>
                {
                    return x.First().Id.ToString();
                })]);
            await wf.Start();
            var res1 = wf.GetTaskResult("ExecSql1");
            var res2 = wf.GetTaskResult("write1");
            Assert.NotNull(res1);
            Assert.NotNull(res2);
            Assert.Equal(value.ToString(), res1.ToString());
        }

        [Fact]
        public async Task ConnectToDbReturnValue()
        {
            GlobalContext globalContext = new GlobalContext(_serviceProvider, _loggerFactory);
            Workflow wf = new Workflow("test", globalContext);
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
                [new ResultBinding<IEnumerable<SqlResultTest>, string>("Message", "ExecSql", (globalContext, x) =>
                {
                    return x.First().Id.ToString();
                })]);
            wf.AddTask<ConsoleWrite, ConsoleWriteParams>("write2",
                new ConsoleWriteParams(),
                [new ResultBinding<object, string>("Message", "ExecSql2", (globalContext, x) =>
                {
                    return x?.ToString()??string.Empty;
                })]);
            await wf.Start();
            var res1 = wf.GetTaskResult("write1");
            var res2 = wf.GetTaskResult("write2");
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
            GlobalContext globalContext = new GlobalContext(_serviceProvider, _loggerFactory);
            ExportExcelFlow<SqlResultTest> excport = new ExportExcelFlow<SqlResultTest>(@params, globalContext, log);
            await excport.Execute();
        }

        [Fact]
        public async Task ExportExcelDatareader()
        {
            GlobalContext globalContext = new GlobalContext(_serviceProvider, _loggerFactory);
            Workflow wf = new Workflow("test", globalContext);
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
            GlobalContext globalContext = new GlobalContext(_serviceProvider, _loggerFactory);
            Workflow wf = new Workflow("test", globalContext);
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
            GlobalContext globalContext = new GlobalContext(_serviceProvider, _loggerFactory);
            Workflow wf = new Workflow("test", globalContext);
            wf.AddTask<FileZipper, FileZipperParams>("ZipFolder",
                new FileZipperParams()
                {
                    ZipAction=ZipAction.Zip,
                    CompressionLevel=CompressionLevel.Optimal,
                    Source=@".\files\zip",
                    Destination=@".\files\test123.zip",
                    IncludeBaseDirectory=true

                });
            await wf.Start();
        }

        [Fact]
        public async Task ZipFolder_DoNot_IncludeBaseDirectory()
        {
            GlobalContext globalContext = new GlobalContext(_serviceProvider, _loggerFactory);
            Workflow wf = new Workflow("test", globalContext);
            wf.AddTask<FileZipper, FileZipperParams>("ZipFolder",
                new FileZipperParams()
                {
                    ZipAction=ZipAction.Zip,
                    CompressionLevel=CompressionLevel.Optimal,
                    Source=@".\files\zip",
                    Destination=@".\files\test123.zip",
                    IncludeBaseDirectory=false

                });
            await wf.Start();
        }

        [Fact]
        public async Task UnzipFile()
        {
            GlobalContext globalContext = new GlobalContext(_serviceProvider, _loggerFactory);
            Workflow wf = new Workflow("test", globalContext);
            wf.AddTask<FileZipper, FileZipperParams>("ZipFolder",
                new FileZipperParams()
                {
                    ZipAction=ZipAction.Unzip,
                    CompressionLevel=CompressionLevel.Optimal,
                    Source=@".\files\test123.zip",
                    Destination=@".\files\unzip",
                    IncludeBaseDirectory=true

                });
            await wf.Start();
        }

        [Fact]
        public async Task TestException()
        {
            GlobalContext globalContext = new GlobalContext(_serviceProvider, _loggerFactory);
            Workflow wf = new Workflow("test", globalContext);
            wf.AddTask<RaiseExceptionTask, RaiseExceptionTaskParams>("ExceptionThrower", new RaiseExceptionTaskParams()
            {
                ExceptionMessage="This is a test exception"
            });
            try
            {
                await wf.Start();
                throw new Exception("You shouldn't be here");
            }
            catch (Exception)
            {
                Assert.True(wf.HasFailures);
                Assert.False(wf.AllResultsSuccessful);
                var failure = wf.MostRecentFailure;
                Assert.True(failure.Message=="This is a test exception");
            }
        }

        [Trait("Category", "Integration")]
        public async Task DownloadReport_FromSsrs_Succeeds()
        {
            GlobalContext globalContext = new GlobalContext(_serviceProvider, _loggerFactory);
            Workflow wf = new Workflow("DownloadReport", globalContext);
            wf.AddTask<ReportServerGetFile, ReportServerGetFileParams>("DownloadReport", new ReportServerGetFileParams()
            {
                UseHttps=false,
                ServerUrl=@"",
                Domain="",
                UserName="",
                Password="",
                ReportPath=@"",
                Format=ReportFormat.PDF,
                Parameters=new Dictionary<string, string>()
                {
                    { "PrintJob", "1" }
                }
            });
            wf.AddTask<WriteBytesToFile, WriteBytesToFileParams>("WriteReportToDisk", new WriteBytesToFileParams()
            {
                Filename="report.pdf",
                Folder=@"C:\temp",
                Timestamp="yyyyMMdd HHmmss"
            }, [new ResultBinding("FileContent", "DownloadReport")]);
            try
            {
                await wf.Start();
            }
            catch (Exception ex)
            {
                Assert.True(wf.HasFailures);
                Assert.False(wf.AllResultsSuccessful);
                var failure = wf.MostRecentFailure;
            }
        }
    }
}