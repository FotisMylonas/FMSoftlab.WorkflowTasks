using Dapper;
using FMSoftlab.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExecutionContext = FMSoftlab.DataAccess.ExecutionContext;

namespace FMSoftlab.WorkflowTasks
{
    public class ExecuteSQLParams : TaskParamsBase
    {
        public bool Scalar { get; set; }
        public bool MultiRow { get; set; }
        public string ConnectionString { get; set; }
        public int CommandTimeout { get; set; }
        public string Sql { get; set; }
        public CommandType CommandType { get; set; }
        public object ExecutionParams { get; set; }

        public ExecuteSQLParams()
        {
            MultiRow=true;
            Scalar=false;
        }
        public override void LoadResults(IGlobalContext globalContext)
        {
            _bindings.SetValueIfBindingExists<object>("QueryParameters", globalContext, (value) => ExecutionParams=value);
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
            try
            {
                object res = null;
                if (TaskParams.ExecutionParams is null)
                    _log?.LogDebug($"no params, {sql}");
                else
                    _log?.LogDebug($"params exist, {sql}");
                IExecutionContext executionContext = new ExecutionContext(TaskParams.ConnectionString, TaskParams.CommandTimeout, IsolationLevel.ReadCommitted);
                if (!TaskParams.Scalar)
                {
                    IEnumerable<dynamic> dbres = null;
                    SqlExecution sqlExecution = new SqlExecution(executionContext, sql, TaskParams.ExecutionParams, TaskParams.CommandType, _log);
                    dbres = await sqlExecution.Query();
                    _log?.LogDebug($"Step:{Name}, ExecuteSQL, executed query {sql}, MultiRow:{TaskParams.MultiRow}, rows returned:{dbres?.Count()}");
                    res=dbres;
                    if (dbres!=null && !TaskParams.MultiRow)
                    {
                        res=dbres.SingleOrDefault();
                    }
                }
                else
                {
                    SqlExecution sqlExecution = new SqlExecution(executionContext, sql, TaskParams.ExecutionParams, TaskParams.CommandType, _log);
                    res = await sqlExecution.ExecuteScalar();
                    _log?.LogDebug($"Step:{Name}, executed scalar {sql}, MultiRow:{TaskParams.MultiRow}, res:{res}");
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
            try
            {
                object res = null;
                IEnumerable<TSqlResults> dbres = null;
                if (TaskParams.ExecutionParams is null)
                    _log?.LogDebug($"no params, {sql}");
                else
                    _log?.LogDebug($"params exist, {sql}");
                IExecutionContext executionContext = new ExecutionContext(TaskParams.ConnectionString, TaskParams.CommandTimeout, IsolationLevel.ReadCommitted);
                SqlExecution sqlExecution = new SqlExecution(executionContext, sql, TaskParams.ExecutionParams, TaskParams.CommandType, _log);
                dbres = await sqlExecution.Query<TSqlResults>();
                _log?.LogDebug($"Step:{Name}, ExecuteSQLTyped, executed {sql}, MultiRow:{TaskParams.MultiRow}, rows returned:{dbres?.Count()}");
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