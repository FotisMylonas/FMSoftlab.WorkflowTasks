using FMSoftlab.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using MiniExcelLibs;
using MiniExcelLibs.OpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FMSoftlab.WorkflowTasks
{
    public class RenderExcelTemplateParams : TaskParamsBase
    {
        public byte[] TemplateContent { get; set; }
        public IDataReader DataReader { get; set; }
        public IEnumerable<object> RenderingData { get; set; }
        public string DataRoot { get; set; }
        public OpenXmlConfiguration OpenXmlConfiguration { get; set; }
        public RenderExcelTemplateParams(IEnumerable<InputBinding> bindings) : base(bindings)
        {

        }
        public RenderExcelTemplateParams() : this(Enumerable.Empty<InputBinding>())
        {

        }
        public override void LoadResults(IGlobalContext globalContext)
        {
            _bindings.SetValueIfBindingExists<IEnumerable<object>>("RenderingData", globalContext, (globalContext, value) => RenderingData = value);
            _bindings.SetValueIfBindingExists<byte[]>("TemplateContent", globalContext, (globalContext, value) => TemplateContent = value);
            _bindings.SetValueIfBindingExists<IDataReader>("DataReader", globalContext, (globalContext, value) => DataReader = value);
        }
    }
    public class RenderExcelTemplate : BaseTaskWithParams<RenderExcelTemplateParams>
    {
        public RenderExcelTemplate(string name, IGlobalContext globalContext, RenderExcelTemplateParams taskParams, ILogger<RenderExcelTemplate> log) : base(name, globalContext, taskParams, log)
        {

        }

        public RenderExcelTemplate(string name, IGlobalContext globalContext, BaseTask parent, RenderExcelTemplateParams taskParams, ILogger<RenderExcelTemplate> log) : base(name, globalContext, parent, taskParams, log)
        {

        }

        private void DisposeReader()
        {
            if (TaskParams.DataReader is null)
                return;
            try
            {
                _log?.LogTrace($"RenderExcelTemplate, closing datareader...");
                TaskParams.DataReader.Close();
            }
            catch (Exception ex)
            {
                _log?.LogAllErrors(ex);
            }
            finally
            {
                _log?.LogTrace($"RenderExcelTemplate, disposing datareader...");
                TaskParams.DataReader.Dispose();
                TaskParams.DataReader = null;
            }
        }
        public override async Task Execute()
        {
            if (TaskParams is null)
            {
                _log?.LogDebug($"{Name} TaskParams is null, exiting");
                return;
            }
            if (TaskParams.RenderingData is null && TaskParams.DataReader is null)
            {
                _log?.LogWarning($"RenderExcelTemplate, no data to render, exiting");
                return;
            }
            if (TaskParams.TemplateContent.Length<=0)
            {
                _log?.LogWarning($"RenderExcelTemplate, template does not exist, exiting");
                return;
            }
            _log?.LogDebug($"RenderExcelTemplate, template length:{TaskParams?.TemplateContent?.Length}");
            byte[] res = new byte[0] { };
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    IDictionary<string, object> rdata = new Dictionary<string, object>();
                    if (TaskParams.DataReader!=null)
                    {
                        rdata.Add(TaskParams.DataRoot, TaskParams.DataReader);
                        _log?.LogDebug($"Will render excel using datareader...");
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(TaskParams.DataRoot))
                            TaskParams.DataRoot ="reportdata";
                        rdata.Add(TaskParams.DataRoot, TaskParams.RenderingData);
                        _log?.LogDebug($"Will render {TaskParams?.RenderingData?.Count()} rows...");
                    }
                    try
                    {
                        OpenXmlConfiguration config = TaskParams.OpenXmlConfiguration!=null ? TaskParams.OpenXmlConfiguration : new OpenXmlConfiguration();
                        string json = JsonSerializer.Serialize(config);
                        _log.LogInformation("MiniExcel settings: {json}", json);
                        await MiniExcel.SaveAsByTemplateAsync(ms, TaskParams.TemplateContent, rdata, config);
                    }
                    finally
                    {
                        DisposeReader();
                    }
                    res = ms.ToArray();
                }
                _log?.LogInformation($"Render excel success, byte array length:{res.Length}");
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