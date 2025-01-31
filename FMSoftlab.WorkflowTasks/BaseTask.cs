using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Dapper.SqlMapper;

namespace FMSoftlab.WorkflowTasks
{
    public class StepResult
    {
        public string StepName { get; set; }
        public string ResultName { get; set; }
        public object ResultValue { get; set; }
    }
    public interface IGlobalContext
    {
        //public IDictionary<string, object> TaskParameters { get; }
        ILoggerFactory LoggerFactory { get; }
        List<StepResult> Results { get; }
        object GetTaskVariable(string stepName, string resultName);
        object GetTaskResult(string stepName);
        object GetGlobalVariable(string resultName);
        void SetTaskVariable(string stepName, string resultName, object Value);
        void SetTaskResult(string stepName, object value);
        void SetGlobalVariable(string resultName, object value);
    }
    public class GlobalContext : IGlobalContext
    {
        public ILoggerFactory LoggerFactory { get; }
        public List<StepResult> Results { get; }

        public GlobalContext(ILoggerFactory loggerFactory)
        {
            LoggerFactory=loggerFactory;
            Results = new List<StepResult>();
        }
        public object GetTaskResult(string stepName)
        {
            return GetTaskVariable(stepName, "Result");
        }
        public object GetTaskVariable(string stepName, string variableName)
        {
            object res = null;
            var item = Results.FirstOrDefault(
                x =>
                string.Equals(x.StepName, stepName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.ResultName, variableName, StringComparison.OrdinalIgnoreCase)
            );
            if (item != null)
            {
                res=item.ResultValue;
            }
            return res;
        }
        public object GetGlobalVariable(string resultName)
        {
            return GetTaskVariable("Global", resultName);
        }
        public void SetTaskResult(string stepName, object value)
        {
            SetTaskVariable(stepName, "Result", value);
        }
        public void SetTaskVariable(string stepName, string variableName, object value)
        {
            var item = Results.FirstOrDefault(
                x =>
                string.Equals(x.StepName, stepName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.ResultName, variableName, StringComparison.OrdinalIgnoreCase)
            );
            if (item!=null)
            {
                item.ResultValue = value;
            }
            else
            {
                Results.Add(new StepResult { StepName=stepName, ResultName=variableName, ResultValue=value });
            }
        }
        public void SetGlobalVariable(string resultName, object value)
        {
            SetTaskVariable("Global", resultName, value);
        }
    }
    public abstract class BaseTask
    {
        protected TaskParamsBase _taskParams;
        public string Name { get; }
        protected readonly ILogger _log;
        public IGlobalContext GlobalContext { get; set; }
        public BaseTask Parent { get; set; }
        public List<BaseTask> Tasks { get; }
        //public IDictionary<string, object> Results { get; set; }

        public BaseTask AddTask(BaseTask executionTask)
        {
            Tasks.Add(executionTask);
            executionTask.Parent = Parent;
            return executionTask;
        }

        public T AddTask<T, TTaskParams>(string name, TTaskParams taskParams, ILogger<T> log) where T : BaseTask where TTaskParams : TaskParamsBase
        {
            T executionTask = (T)Activator.CreateInstance(typeof(T), name, GlobalContext, this, taskParams, log);
            //executionTask.SetParams(taskParams);
            Tasks.Add(executionTask);
            return executionTask;
        }
        public BaseTask(string name, IGlobalContext globalContext, BaseTask parent, ILogger log)
        {
            Name=name;
            Parent =parent;
            Tasks=new List<BaseTask>();
            //Results=new Dictionary<string, object>();
            GlobalContext=globalContext;
            _log=log;
            _taskParams=null;
        }

        public BaseTask(string name, IGlobalContext globalContext, ILogger log)
        {
            Name=name;
            Tasks=new List<BaseTask>();
            //Results=new Dictionary<string, object>();
            GlobalContext=globalContext;
            _log=log;
            _taskParams=null;
        }

        public object GetGlobalVariable(string resultName)
        {
            return GlobalContext.GetGlobalVariable(resultName);
        }
        public object GetTaskVariable(string task, string variable)
        {
            return GlobalContext.GetTaskVariable(task, variable);
        }
        public void SetGlobalVariable(string name, object value)
        {
            GlobalContext.SetGlobalVariable(name, value);
        }
        public void SetTaskResult(object value)
        {
            GlobalContext.SetTaskResult(Name, value);
        }
        private string GetPublicProperties(object obj)
        {
            string res = string.Empty;
            if (obj != null)
            {
                Type type = obj.GetType();
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                if (properties?.Count()>0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (PropertyInfo property in properties)
                    {
                        object value = property.GetValue(obj);
                        if (value != null)
                        {
                            if (value is IDictionary<string, object> dict)
                            {
                                foreach (var dictItem in dict)
                                {
                                    sb.AppendLine($"{dictItem.Key}:{dictItem.Value}");
                                }
                            }
                            else
                            {
                                sb.AppendLine($"{property.Name}:{property.GetValue(obj)}");
                            }
                        }
                    }
                    res=sb.ToString();
                }
            }
            return res;
        }

        public async Task DoExecute()
        {
            _log?.LogDebug($"Starting execution of step:{Name}");
            try
            {
                if (_taskParams!=null)
                {
                    _taskParams.LoadResults(GlobalContext);
                    string s = GetPublicProperties(_taskParams);
                    _log?.LogDebug($"executing step:{Name}, params:{Environment.NewLine}{s}");
                }
                else
                {
                    _log?.LogDebug($"executing step:{Name}, no params");
                }
                await Execute();
            }
            catch (Exception ex)
            {
                _log?.LogError($"Error at step {Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                throw;
            }
            _log?.LogDebug($"Completed step: {Name}");
        }
        public abstract Task Execute();

        public TExecutionTask AddTask<TExecutionTask, TTaskParams>(string name, TTaskParams taskParams) where TExecutionTask : BaseTask where TTaskParams : TaskParamsBase
        {
            TExecutionTask executionTask = (TExecutionTask)Activator.CreateInstance(typeof(TExecutionTask), name, GlobalContext, this, taskParams, GlobalContext.LoggerFactory.CreateLogger<TExecutionTask>());
            Tasks.Add(executionTask);
            return executionTask;
        }
        public TExecutionTask AddTask<TExecutionTask, TTaskParams>(string name, TTaskParams taskParams, IEnumerable<InputBinding> bindings) where TExecutionTask : BaseTask where TTaskParams : TaskParamsBase
        {
            taskParams.LoadBindings(bindings);
            TExecutionTask executionTask = (TExecutionTask)Activator.CreateInstance(typeof(TExecutionTask), name, GlobalContext, this, taskParams, GlobalContext.LoggerFactory.CreateLogger<TExecutionTask>());
            Tasks.Add(executionTask);
            return executionTask;
        }
        public ExecuteSQL AddTask(string name, ExecuteSQLParams taskParams, IEnumerable<InputBinding> bindings)
        {
            return AddTask<ExecuteSQL, ExecuteSQLParams>(name, taskParams, bindings);
        }
    }

    public abstract class BaseTaskWithParams<TTaskParams> : BaseTask where TTaskParams : TaskParamsBase
    {

        public TTaskParams TaskParams { get; set; }

        public BaseTaskWithParams(string name, IGlobalContext globalContext, BaseTask parent, TTaskParams taskParams, ILogger log) : base(name, globalContext, parent, log)
        {
            TaskParams=taskParams;
            _taskParams=taskParams;
        }

        public BaseTaskWithParams(string name, IGlobalContext globalContext, TTaskParams taskParams, ILogger log) : base(name, globalContext, log)
        {
            TaskParams=taskParams;
            _taskParams=taskParams;
        }

        public void SetParams(TTaskParams taskParams)
        {
            TaskParams=taskParams;
            _taskParams=taskParams;
        }

    }
}