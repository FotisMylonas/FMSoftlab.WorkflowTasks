using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.WorkflowTasks
{
    public class InputBinding
    {
        protected readonly TransformationPipeline _pipeline = new TransformationPipeline();
        public string SourceTask { get; set; }
        public string SourceVariable { get; set; }
        public string TargetVariable { get; set; }
        public InputBinding(string targetVariable, string sourceTask, string sourceVariable)
        {
            TargetVariable = targetVariable;
            SourceTask = sourceTask;
            SourceVariable = sourceVariable;
        }
        public void SetValueIfBindingExists<TOutput>(string targetVariable, IGlobalContext globalContext, Action<IGlobalContext, TOutput> setValueAction)
        {
            object tempdata = globalContext.GetTaskVariable(SourceTask, SourceVariable);
            if (tempdata != null)
            {
                object res = _pipeline.Execute(globalContext, tempdata);
                TOutput result = (TOutput)res;
                //TOutput res = Transform(tempdata);
                setValueAction(globalContext, result);
            }
        }

        /*public void SetValueIfBindingExists<TOutput>(string targetVariable, IGlobalContext globalContext, Action<TOutput> setValueAction)
        {
            object tempdata = globalContext.GetTaskVariable(SourceTask, SourceVariable);
            if (tempdata != null)
            {
                object res = _pipeline.Execute(globalContext, tempdata);
                TOutput result = (TOutput)res;
                //TOutput res = Transform(tempdata);
                setValueAction(result);
            }
        }*/
    }
    public class ResultBinding : InputBinding
    {
        public ResultBinding(string targetVariable, string sourceTask) : base(targetVariable, sourceTask, "Result")
        {

        }
    }
    public class InputBinding<TInput, TOutput> : InputBinding
    {
        public InputBinding(string targetVariable, string sourceTask, string sourceVariable, Func<IGlobalContext, TInput, TOutput> transformation) : base(targetVariable, sourceTask, sourceVariable)
        {
            _pipeline.Add(transformation);
        }
    }
    public class ResultBinding<TInput, TOutput> : ResultBinding
    {
        public ResultBinding(string targetVariable, string sourceTask, Func<IGlobalContext, TInput, TOutput> transformation) : base(targetVariable, sourceTask)
        {
            _pipeline.Add(transformation);
        }
    }

    public class BindingsRegistry
    {
        private readonly ILogger _log;
        private readonly List<InputBinding> _bindings;
        public BindingsRegistry(ILogger log)
        {
            _bindings = new List<InputBinding>();
            _log=log;
        }
        public void Load(IEnumerable<InputBinding> bindings)
        {
            if (bindings is null)
                return;
            if (bindings.Count() <= 0)
                return;
            _bindings.Clear();
            _bindings.AddRange(bindings);
        }
        public void Load(InputBinding binding)
        {
            if (binding is null)
                return;
            _bindings.Clear();
            _bindings.Add(binding);
        }
        public InputBinding FindBinding(string targetVariable)
        {
            InputBinding res = _bindings.FirstOrDefault(w =>
            string.Equals(w.TargetVariable, targetVariable, StringComparison.OrdinalIgnoreCase));
            return res;
        }
        public InputBinding Bind(string targetVariable, string sourceTask, string sourceVariable)
        {
            InputBinding binding = FindBinding(targetVariable);
            if (binding is null)
            {
                binding=new InputBinding(targetVariable, sourceTask, sourceVariable);
                _bindings.Add(binding);
            }
            return binding;
        }
        public InputBinding GetRequiredBinding(string targetVariable)
        {
            InputBinding res = FindBinding(targetVariable);
            if (res is null)
                throw new InvalidOperationException($"binding for {targetVariable} does not exist");
            if (string.IsNullOrWhiteSpace(res.SourceTask) || string.IsNullOrWhiteSpace(res.SourceVariable))
                throw new InvalidOperationException($"binding for {targetVariable} is not configured");
            return res;
        }

        public T GetRequiredBindingAs<T>(string targetVariable, GlobalContext globalContext)
        {
            T res = default(T);
            InputBinding bind = GetRequiredBinding(targetVariable);
            if (bind!=null)
            {
                object tempdata = globalContext.GetTaskVariable(bind.SourceTask, bind.SourceVariable);
                if (tempdata != null)
                {
                    res = (T)tempdata;
                }
            }
            return res;
        }
        public T GetBindingAs<T>(string targetVariable, IGlobalContext globalContext)
        {
            T res = default(T);
            InputBinding bind = FindBinding(targetVariable);
            if (bind!=null)
            {
                object tempdata = globalContext.GetTaskVariable(bind.SourceTask, bind.SourceVariable);
                if (tempdata != null)
                {
                    res = (T)tempdata;
                }
            }
            return res;
        }
        public void SetValueIfBindingExists<TOutput>(string targetVariable, IGlobalContext globalContext, Action<IGlobalContext, TOutput> setValueAction)
        {
            InputBinding bind = FindBinding(targetVariable);
            if (bind!=null)
            {
                bind.SetValueIfBindingExists(targetVariable, globalContext, setValueAction);
            }
        }
    }
}
