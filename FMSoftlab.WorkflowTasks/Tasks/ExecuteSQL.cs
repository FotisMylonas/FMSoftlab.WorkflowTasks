using Dapper;
using FMSoftlab.DataAccess;
using FMSoftlab.WorkflowTasks.Tasks;
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
        public ISingleTransactionManager TransactionManager { get; set; }
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
            _bindings.SetValueIfBindingExists<ISingleTransactionManager>("TransactionManager", globalContext, (value) => TransactionManager=value);
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
            if (TaskParams is null)
            {
                _log?.LogDebug($"{Name} TaskParams is null, exiting");
                return;
            }
            object res = null;
            string sql = TaskParams.Sql;
            if (TaskParams.ExecutionParams is null)
                _log?.LogDebug($"no params, {sql}");
            else
                _log?.LogDebug($"params exist, {sql}");
            SqlExecution sqlExecution = null;
            IExecutionContext executionContext = new ExecutionContext(TaskParams.ConnectionString, TaskParams.CommandTimeout, IsolationLevel.ReadCommitted);
            try
            {
                if (TaskParams.TransactionManager!=null)
                {
                    TaskParams.TransactionManager.BeginTransaction();
                    sqlExecution = new SqlExecution(executionContext, TaskParams.TransactionManager, sql, TaskParams.ExecutionParams, TaskParams.CommandType, _log);
                }
                else
                    sqlExecution = new SqlExecution(executionContext, sql, TaskParams.ExecutionParams, TaskParams.CommandType, _log);
                if (!TaskParams.Scalar)
                {
                    var dbres = await sqlExecution.Query();
                    _log?.LogDebug($"Step:{Name}, ExecuteSQL, executed query {sql}, MultiRow:{TaskParams.MultiRow}, rows returned:{dbres?.Count()}");
                    res=dbres;
                    if (dbres!=null && !TaskParams.MultiRow)
                    {
                        res=dbres.SingleOrDefault();
                    }
                }
                else
                {
                    res = await sqlExecution.ExecuteScalar();
                    _log?.LogDebug($"Step:{Name}, executed scalar {sql}, MultiRow:{TaskParams.MultiRow}, res:{res}");
                }
                SetTaskResult(res);
            }
            catch (Exception ex)
            {
                _log?.LogError($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                throw;
            }
        }
    }

    public class ExecuteSQLReader : BaseTaskWithParams<ExecuteSQLParams>
    {

        public ExecuteSQLReader(string name, IGlobalContext globalContext, BaseTask parent, ExecuteSQLParams taskParams, ILogger log) : base(name, globalContext, parent, taskParams, log)
        {

        }

        public ExecuteSQLReader(string name, IGlobalContext globalContext, ExecuteSQLParams taskParams, ILogger log) : base(name, globalContext, taskParams, log)
        {

        }
        public override async Task Execute()
        {
            if (TaskParams is null)
            {
                _log?.LogDebug($"{Name} TaskParams is null, exiting");
                return;
            }
            IDataReader res = null;
            string sql = TaskParams.Sql;
            if (TaskParams.ExecutionParams is null)
                _log?.LogDebug($"no params, {sql}");
            else
                _log?.LogDebug($"params exist, {sql}");
            SqlExecution sqlExecution = null;
            IExecutionContext executionContext = new ExecutionContext(TaskParams.ConnectionString, TaskParams.CommandTimeout, IsolationLevel.ReadCommitted);
            try
            {
                if (TaskParams.TransactionManager!=null)
                {
                    TaskParams.TransactionManager.BeginTransaction();
                    sqlExecution = new SqlExecution(executionContext, TaskParams.TransactionManager, sql, TaskParams.ExecutionParams, TaskParams.CommandType, _log);
                }
                else
                    sqlExecution = new SqlExecution(executionContext, sql, TaskParams.ExecutionParams, TaskParams.CommandType, _log);
                res = await sqlExecution.ExecuteReader();
                _log?.LogDebug($"Step:{Name}, executed reader {sql}, MultiRow:{TaskParams.MultiRow}, RecordsAffected:{res.RecordsAffected}");
                SetTaskResult(res);
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
            if (TaskParams is null)
            {
                _log?.LogDebug($"{Name} TaskParams is null, exiting");
                return;
            }
            object res = null;
            string sql = TaskParams.Sql;
            if (TaskParams.ExecutionParams is null)
                _log?.LogDebug($"no params, {sql}");
            else
                _log?.LogDebug($"params exist, {sql}");
            SqlExecution sqlExecution = null;
            IEnumerable<TSqlResults> dbres = null;
            IExecutionContext executionContext = new ExecutionContext(TaskParams.ConnectionString, TaskParams.CommandTimeout, IsolationLevel.ReadCommitted);
            try
            {
                if (TaskParams.TransactionManager!=null)
                {
                    TaskParams.TransactionManager.BeginTransaction();
                    sqlExecution =new SqlExecution(executionContext, TaskParams.TransactionManager, sql, TaskParams.ExecutionParams, TaskParams.CommandType, _log);
                }
                else
                    sqlExecution = new SqlExecution(executionContext, sql, TaskParams.ExecutionParams, TaskParams.CommandType, _log);
                if (!TaskParams.Scalar)
                {
                    dbres = await sqlExecution.Query<TSqlResults>();
                    _log?.LogDebug($"Step:{Name}, ExecuteSQLTyped, executed {sql}, MultiRow:{TaskParams.MultiRow}, rows returned:{dbres?.Count()}");
                    res=dbres;
                    if (dbres!=null && !TaskParams.MultiRow)
                    {
                        res=dbres.SingleOrDefault();
                    }
                }
                else
                {
                    res = await sqlExecution.ExecuteScalar<TSqlResults>();
                    _log?.LogDebug($"Step:{Name}, executed scalar {sql}, MultiRow:{TaskParams.MultiRow}, res:{res}");
                }
                SetTaskResult(res);
            }
            catch (Exception ex)
            {
                _log?.LogError($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                throw;
            }
        }
    }
}