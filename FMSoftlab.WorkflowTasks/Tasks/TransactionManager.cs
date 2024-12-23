using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FMSoftlab.DataAccess;
using System.Data;
using ExecutionContext = FMSoftlab.DataAccess.ExecutionContext;

namespace FMSoftlab.WorkflowTasks.Tasks
{
    public class TransactionManagerParams : TaskParamsBase
    {
        public string ConnectionString { get; set; }
        public TransactionManagerParams()
        {
            ConnectionString = string.Empty;
        }

        public TransactionManagerParams(IEnumerable<InputBinding> bindings) : base(bindings)
        {
            ConnectionString = string.Empty;
        }

        public TransactionManagerParams(InputBinding binding) : base(binding)
        {
            ConnectionString = string.Empty;
        }

        public override void LoadResults(IGlobalContext globalContext)
        {

        }
    }
    public class TransactionManager : BaseTaskWithParams<TransactionManagerParams>
    {
        public TransactionManager(string name, IGlobalContext globalContext, TransactionManagerParams settings, ILogger log) : base(name, globalContext, settings, log)
        {

        }

        public TransactionManager(string name, IGlobalContext globalContext, BaseTask parent, TransactionManagerParams settings, ILogger log) : base(name, globalContext, parent, settings, log)
        {

        }

        public override async Task Execute()
        {
            IExecutionContext executionContext = new ExecutionContext(TaskParams.ConnectionString, 30, IsolationLevel.ReadCommitted);
            SingleTransactionManager tm = new SingleTransactionManager(executionContext, _log);
            tm.BeginTransaction();
            SetTaskResult(tm);
            await Task.CompletedTask;
        }
    }

    public class CompleteTransactionParams : TaskParamsBase
    {
        public ISingleTransactionManager TransactionManager { get; set; }
        public CompleteTransactionParams()
        {

        }

        public CompleteTransactionParams(IEnumerable<InputBinding> bindings) : base(bindings)
        {

        }

        public CompleteTransactionParams(InputBinding binding) : base(binding)
        {

        }

        public override void LoadResults(IGlobalContext globalContext)
        {
            _bindings.SetValueIfBindingExists<ISingleTransactionManager>("TransactionManager", globalContext, (value) => TransactionManager = value);
        }
    }

    //rollback transaction
    public class TransactionRollback : BaseTaskWithParams<CompleteTransactionParams>
    {
        public TransactionRollback(string name, IGlobalContext globalContext, CompleteTransactionParams taskParams, ILogger log) : base(name, globalContext, taskParams, log)
        {

        }

        public TransactionRollback(string name, IGlobalContext globalContext, BaseTask parent, CompleteTransactionParams taskParams, ILogger log) : base(name, globalContext, parent, taskParams, log)
        {

        }

        public override async Task Execute()
        {
            try
            {
                TaskParams.TransactionManager.Rollback();
                SetTaskResult(true);
            }
            catch (Exception ex)
            {
                _log?.LogError($"Error at step {Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                throw;
            }
            finally
            {
                TaskParams.TransactionManager.Dispose();
            }
            await Task.CompletedTask;
        }
    }

    //commit
    public class TransactionCommit : BaseTaskWithParams<CompleteTransactionParams>
    {
        public TransactionCommit(string name, IGlobalContext globalContext, CompleteTransactionParams taskParams, ILogger log) : base(name, globalContext, taskParams, log)
        {

        }

        public TransactionCommit(string name, IGlobalContext globalContext, BaseTask parent, CompleteTransactionParams taskParams, ILogger log) : base(name, globalContext, parent, taskParams, log)
        {

        }

        public override async Task Execute()
        {
            try
            {
                TaskParams.TransactionManager.Commit();
                SetTaskResult(true);
            }
            catch (Exception ex)
            {
                _log?.LogError($"Error at step {Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                throw;
            }
            finally
            {
                TaskParams.TransactionManager.Dispose();
            }
            await Task.CompletedTask;
        }
    }
}