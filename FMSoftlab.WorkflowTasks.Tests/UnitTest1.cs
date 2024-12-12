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
            wf.AddTask<ConsoleWrite, ConsoleWriteParams>("write1", 
                new ConsoleWriteParams(), 
                [new InputBinding<IEnumerable<SqlResultTest>, string>("Message", "ExecSql", "Result", (x) =>  
                { 
                    return x.First().Id.ToString();
                })]);
            await wf.Start();
            var res = wf.GetTaskVariable("write1", "Result");
            Assert.NotNull(res);
            Assert.Equal("88888888", res.ToString());
        }
    }
}