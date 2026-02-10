using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http; // Add this using directive
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using static System.Net.WebRequestMethods;

namespace FMSoftlab.WorkflowTasks.Tasks
{
    public enum ReportFormat
    {
        PDF = 0,
        Excel,
        Word,
        HTML
    }
    public class HttpClientCredentialsOptions
    {
        public string Domain { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }        
    }
    public static class ReportServerHttpClientHelpers
    {
        public static void AddReportServerHttpClient(this IServiceCollection services, HttpClientCredentialsOptions httpClientCredentialsOptions)
        {
            // In Startup.cs or Program.cs
            // Register typed HTTP client
            services.AddHttpClient<ReportServerClient>()
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromMinutes(5);
                    client.DefaultRequestHeaders.Add("User-Agent", "ReportFlow/1.0");
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5)) // How long to keep handlers
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    HttpClientHandler handler = null;
                    if (httpClientCredentialsOptions != null && !string.IsNullOrWhiteSpace(httpClientCredentialsOptions.UserName))
                    {
                        handler= new HttpClientHandler()
                        {
                            // Default handler settings
                            UseDefaultCredentials=false,
                            Credentials = new NetworkCredential(
                                httpClientCredentialsOptions.UserName,
                                httpClientCredentialsOptions.Password,
                                httpClientCredentialsOptions.Domain),
                            PreAuthenticate = true,
                            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                        };
                    }
                    else
                    {
                        handler= new HttpClientHandler()
                        {
                            // Default handler settings
                            UseDefaultCredentials=true,
                            PreAuthenticate = true,
                            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                        };
                    }
                    return handler;
                });
        }
    }
    public sealed class ReportServerClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReportServerClient> _log;

        public ReportServerClient(HttpClient httpClient, ILogger<ReportServerClient> log)
        {
            _httpClient = httpClient;
            _log = log;
        }

        public async Task<byte[]> GetRenderedReportAsync(
            bool useHttps,
            string reportServerBaseUrl,
            string reportPath,
            ReportFormat format,
            IDictionary<string, string> parameters = null,
            CancellationToken cancellationToken = default)
        {
            var uri = BuildRenderUri(
                useHttps,
                reportServerBaseUrl,
                reportPath,
                format,
                parameters
            );
            string ssrsUrl = @$"http://{reportServerBaseUrl}/Pages/ReportViewer.aspx?{reportPath}&rs:Command=Render&rs:Format=PDF&PrintJob=1";
            _log.LogInformation("Requesting report from SSRS: {Url}", uri);

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        private static Uri BuildRenderUri(
            bool useHttps,
            string baseUrl,
            string reportPath,
            ReportFormat format,
            IDictionary<string, string> parameters)
        {
            reportPath=Uri.EscapeDataString(reportPath);
            string scheme = useHttps ? "https" : "http";
            string ssrsUrl = @$"{scheme}://{baseUrl.TrimEnd('/')}/Pages/ReportViewer.aspx?{reportPath.TrimEnd('/')}&rs:Command=Render&rs:Format={format}";

            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    ssrsUrl+='&';
                    ssrsUrl+=Uri.EscapeDataString(p.Key);
                    ssrsUrl+='=';
                    ssrsUrl+=Uri.EscapeDataString(p.Value);
                }
            }
            return new Uri(ssrsUrl, UriKind.Absolute);
        }
    }

    public class ReportServerGetFileParams : TaskParamsBase
    {
        public bool UseHttps { get; set; }
        public string ServerUrl { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ReportPath { get; set; } = string.Empty;
        public ReportFormat Format { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public ReportServerGetFileParams(IEnumerable<InputBinding> bindings) : base(bindings)
        {
            ServerUrl = string.Empty;
            UserName = string.Empty;
            Password = string.Empty;
            ReportPath = string.Empty;
            Domain = string.Empty;
            Parameters = new Dictionary<string, string>();
            Format = ReportFormat.PDF;
        }
        public ReportServerGetFileParams() : base()
        {
            ServerUrl = string.Empty;
            UserName = string.Empty;
            Password = string.Empty;
            ReportPath = string.Empty;
            Domain = string.Empty;
            Parameters = new Dictionary<string, string>();
            Format = ReportFormat.PDF;
        }
        public override void LoadResults(IGlobalContext globalContext)
        {
            _bindings.SetValueIfBindingExists<Dictionary<string, string>>(nameof(Parameters), globalContext, (globalContext, value) =>
            {
                Parameters.Clear();
                foreach (var kvp in value)
                {
                    Parameters[kvp.Key] = kvp.Value;
                }
            });
        }
    }
    public class ReportServerGetFile : BaseTaskWithParams<ReportServerGetFileParams>
    {

        public ReportServerGetFile(string name, IGlobalContext globalContext, ReportServerGetFileParams taskParams, ILogger log) : base(name, globalContext, taskParams, log)
        {

        }

        public ReportServerGetFile(string name, IGlobalContext globalContext, BaseTask parent, ReportServerGetFileParams taskParams, ILogger log) : base(name, globalContext, parent, taskParams, log)
        {

        }

        public override async Task Execute()
        {
            if (string.IsNullOrWhiteSpace(TaskParams?.ServerUrl))
            {
                _log?.LogWarning($@"Task:{Name}, ServerUrl is empty, exiting");
                return;
            }

            if (string.IsNullOrWhiteSpace(TaskParams?.ReportPath))
            {
                _log?.LogWarning($@"Task:{Name}, ReportPath is empty, exiting");
            }
            var reportServerHttpClient = _serviceProvider.GetRequiredService<ReportServerClient>();
            var bytes = await reportServerHttpClient.GetRenderedReportAsync(false,
                TaskParams.ServerUrl,
                TaskParams.ReportPath,
                TaskParams.Format, TaskParams.Parameters);
            SetTaskResult(bytes);
        }
    }
}
