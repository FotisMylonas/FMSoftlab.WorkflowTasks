using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static Dapper.SqlMapper;

namespace FMSoftlab.WorkflowTasks
{

    public class LoopListParams<T> : TaskParamsBase
    {
        public List<T> Items { get; set; }

        public LoopListParams()
        {
            Items= new List<T>();
        }

        public LoopListParams(IEnumerable<InputBinding> bindings) : base(bindings)
        {
            Items= new List<T>();
        }

        public LoopListParams(InputBinding binding) : base(binding)
        {
            Items= new List<T>();
        }

        public override void LoadResults(IGlobalContext globalContext)
        {
            _bindings.SetValueIfBindingExists<IEnumerable<T>>("Items", globalContext, (value) =>
            {
                if (value?.Any() ?? false)
                {
                    Items.AddRange(value);
                }
            });
        }
    }

    public class LoopList<TListType> : BaseTaskWithParams<LoopListParams<TListType>>
    {

        public LoopList(string name, IGlobalContext globalContext, LoopListParams<TListType> taskParams, ILogger log) : base(name, globalContext, taskParams, log)
        {

        }

        public LoopList(string name, IGlobalContext globalContext, BaseTask parent, LoopListParams<TListType> taskParams, ILogger log) : base(name, globalContext, parent, taskParams, log)
        {

        }

        public async override Task Execute()
        {
            if (!(TaskParams?.Items.Any()??false))
            {
                return;
            }
            var workerBlock = new ActionBlock<TListType>(async (id) =>
            {
                try
                {
                    //await TaskParams.Flow
                    //await TaskParams.Flow.Start();
                    _log?.LogDebug($"loop list, id:{id}");
                }
                catch (Exception ex)
                {
                    _log?.LogError(ex.Message);
                }
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 5 });
            try
            {
                if (TaskParams?.Items != null && TaskParams?.Items?.Count() > 0)
                {
                    _log?.LogDebug($"items in Queue:{TaskParams?.Items.Count()}");

                    while (TaskParams.Items?.Any() ?? false)
                    {
                        var item = TaskParams.Items.FirstOrDefault();
                        try
                        {
                            await workerBlock.SendAsync(item);
                        }
                        finally
                        {
                            TaskParams.Items.Remove(item);
                        }
                    }
                }
                workerBlock.Complete();
                await workerBlock.Completion;
            }
            catch (Exception e)
            {
                _log?.LogError($"Error: {e.Message}{Environment.NewLine}{e.StackTrace}");
            }
        }
    }
}