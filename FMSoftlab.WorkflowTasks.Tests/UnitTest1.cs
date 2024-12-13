using FMSoftlab.WorkflowTasks.Flows;
using FMSoftlab.WorkflowTasks.Tasks;
using Microsoft.Extensions.Logging;
using System.Data;

namespace FMSoftlab.WorkflowTasks.Tests
{

    public class SqlResultTest
    {
        public int Id { get; set; }
    }
    public class WorkflowTests
    {
        [Fact]
        public async Task Test1()
        {
            ILoggerFactory logfact = new LoggerFactory();
            Workflow wf = new Workflow("test", logfact);
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
                [new InputBinding<IEnumerable<SqlResultTest>, string>("Message", "ExecSql", "Result", (x) =>
                {
                    return x.First().Id.ToString();
                })]);
            wf.AddTask<ConsoleWrite, ConsoleWriteParams>("write2",
                new ConsoleWriteParams(),
                [new InputBinding<object, string>("Message", "ExecSql2", "Result", (x) =>
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
            ILoggerFactory logfact = new LoggerFactory();
            ILogger<ExportExcelFlow<SqlResultTest>> log = logfact.CreateLogger<ExportExcelFlow<SqlResultTest>>();
            IExportExcelFlowParams @params = new ExportExcelFlowParams()
            {
                Name="ExelExportFlow",
                ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true",
                LongRunningTimeout=100,
                ShortRunningTimeout=10,
                ExportFolder="Files",
                Filename="result.xlsx",
                StagingSql=string.Empty,
                StagingSqlCommandType=CommandType.Text,
                ExportSql= "select 1 as id union select 2 union select 3 as Id",
                ExportSqlCommandType=CommandType.Text,
                DataRoot="data",                
                Template=@"Files\test.xlsx"
            };
            ExportExcelFlow<SqlResultTest> excport = new ExportExcelFlow<SqlResultTest>(@params, logfact, log);
            await excport.Execute();
        }
    }
}