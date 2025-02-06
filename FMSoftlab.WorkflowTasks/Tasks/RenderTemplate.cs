using Fluid;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.WorkflowTasks
{
    public class RenderTemplateParams : TaskParamsBase
    {
        public string Template { get; set; }

        public IDictionary<string, object> RenderingData { get; set; }

        public RenderTemplateParams(IEnumerable<InputBinding> bindings) : base(bindings)
        {

        }
        public RenderTemplateParams() : base()
        {

        }

        public override void LoadResults(IGlobalContext globalContext)
        {
            _bindings.SetValueIfBindingExists<string>("Template", globalContext, (globalContext, value) => Template = value);
            _bindings.SetValueIfBindingExists<IDictionary<string, object>>("RenderingData", globalContext, (globalContext, value) => RenderingData = value);
        }
    }
    public class RenderTemplate : BaseTaskWithParams<RenderTemplateParams>
    {
        public RenderTemplate(string name, IGlobalContext globalContext, RenderTemplateParams taskParams, ILogger<RenderTemplate> log) : base(name, globalContext, taskParams, log)
        {

        }

        public RenderTemplate(string name, IGlobalContext globalContext, BaseTask parent, RenderTemplateParams taskParams, ILogger<RenderTemplate> log) : base(name, globalContext, parent, taskParams, log)
        {

        }

        public override async Task Execute()
        {
            if (TaskParams?.RenderingData==null)
            {
                _log?.LogWarning($@"Task:{Name}, RenderingData is empty, exiting");
                return;
            }
            if (!(TaskParams?.RenderingData?.Any() ?? false))
            {
                _log?.LogWarning($@"Task:{Name}, RenderingData is empty, exiting");
                return;
            }
            if (string.IsNullOrWhiteSpace(TaskParams.Template))
            {
                _log?.LogWarning($"Task:{Name}, Template is empty");
                return;
            }
            var parser = new FluidParser();
            if (!parser.TryParse(TaskParams.Template, out var template, out var error))
            {
                _log?.LogError($"Task:{Name}, error:{error}");
                return;
            }
            var options = new TemplateOptions();
            options.MemberAccessStrategy=new UnsafeMemberAccessStrategy();
            foreach (var dict in TaskParams.RenderingData)
            {
                options.MemberAccessStrategy.Register(dict.Value.GetType());
            }
            options.MemberAccessStrategy.IgnoreCasing = true;
            TemplateContext context = new TemplateContext(TaskParams.RenderingData, options);
            string res = template.Render(context);
            SetTaskResult(res);
            await Task.CompletedTask;
        }
    }
}