using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.WorkflowTasks
{
    public class ExecuteSQLParams : TaskParamsBase
    {
        public bool MultiRow { get; set; }
        public string ConnectionString { get; set; }
        public int CommandTimeout { get; set; }
        public string Sql { get; set; }
        public CommandType CommandType { get; set; }
        public IDictionary<string, object> ExecutionParams { get; set; }


        public ExecuteSQLParams()
        {
            ExecutionParams=new Dictionary<string, object>();
            MultiRow=true;
        }

        public override void LoadResults(IGlobalContext globalContext)
        {
            _bindings.SetValueIfBindingExists<IDictionary<string, object>>("QueryParameters", globalContext, (value) => ExecutionParams=value);
        }
    }
    public class ExecuteSQL : BaseTaskWithParams<ExecuteSQLParams>
    {

        public ExecuteSQL(string name, IGlobalContext globalContext, BaseTask parent, ExecuteSQLParams taskParams, ILogger log) : base(name, globalContext, parent, taskParams, log)
        {

        }

        public ExecuteSQL(string name, IGlobalContext globalContext, ExecuteSQLParams taskParams, ILogger log) : base(name, globalContext, taskParams, log)
        {

        }
        public override async Task Execute()
        {
            string sql = TaskParams.Sql;
            using (SqlConnection con = new SqlConnection(TaskParams.ConnectionString))
            {
                try
                {
                    object res = null;
                    IEnumerable<dynamic> dbres = null;
                    if (TaskParams.ExecutionParams!=null && (TaskParams?.ExecutionParams.Any() ?? false))
                    {
                        dbres = await con.QueryAsync(sql, TaskParams.ExecutionParams, commandType: TaskParams.CommandType, commandTimeout: TaskParams.CommandTimeout);
                    }
                    else
                    {
                        dbres = await con.QueryAsync(sql, null, commandType: TaskParams.CommandType, commandTimeout: TaskParams.CommandTimeout);
                    }
                    _log?.LogDebug($"Step:{Name}, executed {sql}, MultiRow:{TaskParams.MultiRow},rows returned:{dbres?.Count()}");
                    res=dbres;
                    if (dbres!=null && !TaskParams.MultiRow)
                    {
                        res=dbres.SingleOrDefault();
                    }
                    GlobalContext.SetTaskVariable(Name, "Result", res);

                }
                catch (Exception ex)
                {
                    _log?.LogError($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                    throw;
                }
            }
        }
    }

    public class ExecuteSQLTyped<TSqlResults> : BaseTaskWithParams<ExecuteSQLParams>
    {

        public ExecuteSQLTyped(string name, IGlobalContext globalContext, BaseTask parent, ExecuteSQLParams taskParams, ILogger log) : base(name, globalContext, parent, taskParams, log)
        {

        }

        public ExecuteSQLTyped(string name, IGlobalContext globalContext, ExecuteSQLParams taskParams, ILogger log) : base(name, globalContext, taskParams, log)
        {

        }
        public override async Task Execute()
        {
            string sql = TaskParams.Sql;
            using (SqlConnection con = new SqlConnection(TaskParams.ConnectionString))
            {
                try
                {
                    object res = null;
                    IEnumerable<TSqlResults> dbres = null;
                    if (TaskParams.ExecutionParams!=null && (TaskParams?.ExecutionParams.Any() ?? false))
                    {
                        dbres = await con.QueryAsync<TSqlResults>(sql, TaskParams.ExecutionParams, commandType: TaskParams.CommandType);
                    }
                    else
                    {
                        dbres = await con.QueryAsync<TSqlResults>(sql, null, commandType: TaskParams.CommandType);

                    }
                    _log?.LogDebug($"Step:{Name}, executed {sql}, MultiRow:{TaskParams.MultiRow},rows returned:{dbres?.Count()}");
                    res=dbres;
                    if (dbres!=null && !TaskParams.MultiRow)
                    {
                        res=dbres.SingleOrDefault();
                    }
                    GlobalContext.SetTaskVariable(Name, "Result", res);

                }
                catch (Exception ex)
                {
                    _log?.LogError($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                    throw;
                }
            }
        }
    }
}