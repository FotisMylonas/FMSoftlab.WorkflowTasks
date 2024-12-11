using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FMSoftlab.WorkflowTasks
{

    /* public static class ObjectHelpers
     {
         public static void LoadFromDictionary(this object obj, IDictionary<string, object> dictionary)
         {
             var objectType = obj.GetType();
             var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

             foreach (var property in properties)
             {
                 if (property.CanWrite && dictionary.ContainsKey(property.Name))
                 {
                     var value = dictionary[property.Name];
                     if (value.GetType() == property.PropertyType)
                     {
                         property.SetValue(obj, value);
                     }
                     else
                     {
                         // handle type mismatch if needed
                         // e.g. convert the value to the appropriate type
                     }
                 }
             }
         }
         public static IDictionary<string, object> SaveToDictionary(this object obj)
         {
             var dictionary = new Dictionary<string, object>();
             var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

             foreach (var property in properties)
             {
                 if (property.CanRead)
                 {
                     dictionary.Add(property.Name, property.GetValue(obj));
                 }
             }
             return dictionary;
         }
     }*/

    public class InputBinding
    {
        public string SourceTask { get; set; }
        public string SourceVariable { get; set; }
        public string TargetVariable { get; set; }
        public InputBinding(string targetVariable, string sourceTask, string sourceVariable)
        {
            TargetVariable = targetVariable;
            SourceTask = sourceTask;
            SourceVariable = sourceVariable;
        }
    }
    public class InputBinding<TInput, TOutput> : InputBinding
    {
        private readonly Func<TInput, TOutput> _transformation;
        public InputBinding(string targetVariable, string sourceTask, string sourceVariable, Func<TInput, TOutput> transformation) : base(targetVariable, sourceTask, sourceVariable)
        {
            _transformation=transformation;
        }
    }
    public class BindingsRegistry
    {
        private readonly List<InputBinding> _bindings;
        public BindingsRegistry()
        {
            _bindings = new List<InputBinding>();
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
            if (binding!=null)
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
        public void SetValueIfBindingExists<T>(string targetVariable, IGlobalContext globalContext, Action<T> setValueAction)
        {
            InputBinding bind = FindBinding(targetVariable);
            if (bind is null)
                return;
            object tempdata = globalContext.GetTaskVariable(bind.SourceTask, bind.SourceVariable);
            if (tempdata != null)
                if (tempdata is T res)
                    setValueAction(res);
        }
    }

    public abstract class TaskParamsBase
    {
        protected readonly BindingsRegistry _bindings;
        public TaskParamsBase()
        {
            _bindings=new BindingsRegistry();
        }
        public TaskParamsBase(IEnumerable<InputBinding> bindings)
        {
            _bindings=new BindingsRegistry();
            if (bindings?.Any()??false)
                _bindings.Load(bindings);
        }
        public TaskParamsBase(InputBinding binding)
        {
            _bindings=new BindingsRegistry();
            if (binding!=null)
                _bindings.Load(binding);
        }

        public void LoadBindings(IEnumerable<InputBinding> bindings)
        {
            _bindings.Load(bindings);
        }
        public abstract void LoadResults(IGlobalContext globalContext);
    }
}