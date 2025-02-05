using Microsoft.Extensions.Logging;
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

    public class TransformationPipeline
    {
        private readonly List<Func<IGlobalContext, object, object>> _transformations = new();

        // Add transformation step
        public TransformationPipeline Add<TInput, TOutput>(Func<IGlobalContext, TInput, TOutput> transformation)
        {
            _transformations.Add((globalContext, input) => transformation(globalContext, (TInput)input));
            return this;
        }

        // Execute all transformations
        public object Execute(IGlobalContext globalContext, object initialValue)
        {
            object result = initialValue;
            foreach (var transform in _transformations)
            {
                result = transform(globalContext, result);
            }
            return result;
        }
    }
 
    public abstract class TaskParamsBase
    {
        protected readonly BindingsRegistry _bindings;
        protected readonly ILogger _log;
        public TaskParamsBase()
        {
            _log=null;
            _bindings=new BindingsRegistry(_log);
        }
        public TaskParamsBase(IEnumerable<InputBinding> bindings)
        {
            _log=null;
            _bindings=new BindingsRegistry(_log);
            if (bindings?.Any()??false)
                _bindings.Load(bindings);
        }
        public TaskParamsBase(InputBinding binding)
        {
            _log=null;
            _bindings=new BindingsRegistry(_log);
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