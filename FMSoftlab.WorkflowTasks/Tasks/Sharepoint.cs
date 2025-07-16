using Azure.Identity;
using FMSoftlab.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DriveUpload = Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;

namespace FMSoftlab.WorkflowTasks.Tasks
{


    public class SharepointTaskParams : TaskParamsBase
    {
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string SiteId { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public string LibraryId { get; set; } = string.Empty;
        public string LibraryName { get; set; } = string.Empty;
        public string DestinationFolder { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public byte[] FileContent { get; set; } = [];
        public SharepointTaskParams(IEnumerable<InputBinding> bindings) : base(bindings)
        {

        }
        public SharepointTaskParams() : this(Enumerable.Empty<InputBinding>())
        {

        }
        public override void LoadResults(IGlobalContext globalContext)
        {
            _bindings.SetValueIfBindingExists<byte[]>("FileContent", globalContext, (globalContext, value) => FileContent = value);
        }
    }

    public class SharepointTask : BaseTaskWithParams<SharepointTaskParams>
    {
        public SharepointTask(string name, IGlobalContext globalContext, SharepointTaskParams taskParams, ILogger log) : base(name, globalContext, taskParams, log)
        {
        }

        public SharepointTask(string name, IGlobalContext globalContext, BaseTask parent, SharepointTaskParams taskParams, ILogger log) : base(name, globalContext, parent, taskParams, log)
        {

        }

        private async Task UploadFileToSharepoint()
        {
            var confidentialClient = ConfidentialClientApplicationBuilder
                .Create(TaskParams.ClientId)
                .WithTenantId(TaskParams.TenantId)
                .WithClientSecret(TaskParams.ClientSecret)
                .Build();


            // using Azure.Identity;
            var options = new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            };

            // https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var clientSecretCredential = new ClientSecretCredential(TaskParams.TenantId, TaskParams.ClientId, TaskParams.ClientSecret, options);

            var uploadSessionRequestBody = new DriveUpload.CreateUploadSessionPostRequestBody
            {
                Item = new DriveItemUploadableProperties
                {
                    AdditionalData = new Dictionary<string, object>
                {
                    { "@microsoft.graph.conflictBehavior", "replace" },
                },
                },
            };

            var graphClient = new GraphServiceClient(clientSecretCredential);
            var sites = await graphClient.Sites.GetAsync();
            if (sites is null || sites.Value is null)
            {
                _log?.LogWarning("graphClient, no sites");
                return;
            }
            foreach (var s in sites.Value)
            {
                _log?.LogTrace("Site: {Id}, Name: {SiteName}", s.Id, s.Name);
            }
            Site site = null;
            if (!string.IsNullOrWhiteSpace(TaskParams.SiteName))
            {
                _log?.LogDebug("Will search for site with name: {SiteName}", TaskParams.SiteName);
                site =sites.Value.Where(w => string.Equals(w.Name, TaskParams.SiteName)).SingleOrDefault();
            }
            else
            {
                _log?.LogDebug("Will search for site with id: {SiteId}", TaskParams.SiteId);
                site=sites.Value.Where(w => string.Equals(w.Name, TaskParams.SiteId)).SingleOrDefault();
            }
            if (site is null)
            {
                _log?.LogWarning("graphClient, no site was found, SiteId: {SiteId}, SiteName: {SiteName}", TaskParams.SiteId, TaskParams.SiteName);
                return;
            }
            var drives = await graphClient.Sites[site.Id].Drives.GetAsync();
            if (drives is null || drives.Value is null)
            {
                _log?.LogWarning("graphClient, no drives");
                return;
            }
            foreach (var d in drives.Value)
            {
                _log?.LogTrace("Drive: {Id}, Name: {Name}", d.Id, d.Name);
            }
            var drive = drives.Value.Where(w => string.Equals(w.Name, "OneDrive", StringComparison.OrdinalIgnoreCase)).SingleOrDefault();
            if (drive is null)
            {
                _log?.LogWarning("No drive was found, SiteId: {SiteId}, SiteName: {SiteName}, LibraryId: {LibraryId}, LibraryName: {LibraryName}",
                    TaskParams.SiteId,
                    TaskParams.SiteName,
                    TaskParams.LibraryId,
                    TaskParams.LibraryName);
                return;
            }
            string uploadPath = "/"+TaskParams.DestinationFolder.TrimEnd('/').TrimStart('/') +"/"+Path.GetFileName(TaskParams.FileName);
            _log?.LogInformation("Saving to: {uploadPath}", uploadPath);
            var uploadSession = await graphClient.Drives[drive.Id]
                .Items["root"]
                .ItemWithPath(uploadPath)
                .CreateUploadSession
                .PostAsync(uploadSessionRequestBody);

            using Stream fileStream = TaskParams.FileContent?.Length > 0
                ? new MemoryStream(TaskParams.FileContent)
                : File.OpenRead(TaskParams.FileName);
            // Max slice size must be a multiple of 320 KiB
            int maxSliceSize = 320 * 1024;
            var fileUploadTask = new LargeFileUploadTask<DriveItem>(
                uploadSession, fileStream, maxSliceSize, graphClient.RequestAdapter);

            var totalLength = fileStream.Length;
            // Create a callback that is invoked after each slice is uploaded
            IProgress<long> progress = new Progress<long>(prog =>
            {
                _log?.LogDebug($"Uploaded {prog} bytes of {totalLength} bytes");
            });

            try
            {
                var uploadResult = await fileUploadTask.UploadAsync(progress);
                _log?.LogInformation(uploadResult.UploadSucceeded
                    ? $"Upload complete, item ID: {uploadResult.ItemResponse.Id}"
                    : "Upload failed");
            }
            catch (ODataError ex)
            {
                _log?.LogError($"Error uploading: {ex.Error?.Message}");
                _log?.LogAllErrors(ex);
            }
        }
        public override async Task Execute()
        {
            if (TaskParams is null)
            {
                _log?.LogDebug($"{Name} TaskParams is null, exiting");
                return;
            }
            if (TaskParams.TenantId is null)
            {
                _log?.LogWarning($"No TenantId");
                return;
            }

            if (TaskParams.ClientId is null)
            {
                _log?.LogWarning($"No ClientId");
                return;
            }

            if (TaskParams.ClientSecret is null)
            {
                _log?.LogWarning($"No ClientSecret");
                return;
            }

            if (TaskParams.FileContent.Length<=0 && string.IsNullOrWhiteSpace(TaskParams.FileName))
            {
                _log?.LogWarning($"No FileContent or FileName");
                return;
            }

            _log.LogInformation("TenantId: {TenantId}, ClientId: {ClientId}, ClientSecret: {ClientSecret}, SiteName: {SiteName}, SiteId: {SiteId}, LibraryName: {LibraryName}, LibraryId: {LibraryId}, Filename: {Filename}, Content length: {FileContentLength}",
                TaskParams.TenantId,
                TaskParams.ClientId,
                TaskParams.ClientSecret,
                TaskParams.SiteName,
                TaskParams.SiteId,
                TaskParams.LibraryName,
                TaskParams.LibraryId,
                TaskParams.FileName,
                TaskParams.FileContent?.Length
                );
            await UploadFileToSharepoint();
        }
    }
}