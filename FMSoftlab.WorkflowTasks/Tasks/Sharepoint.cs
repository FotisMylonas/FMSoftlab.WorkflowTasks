using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.Graph.Models;
using Azure.Identity;

namespace FMSoftlab.WorkflowTasks.Tasks
{

    //https://www.sharepointpals.com/post/upload-file-to-sharepoint-office-365-programmatically-using-c-csom-_api-web-getfolderbyserverrelativeurl-postasync-httpclient/
    //https://www.sharepointpals.com/post/upload-file-to-sharepoint-office-365-programmatically-using-c-csom-pnp/


    public class SharePointUploader
    {
        private readonly GraphServiceClient _graphClient;
        private readonly string _siteId; // SharePoint site ID
        private readonly string _clientId; // Azure AD App Client ID
        private readonly string _tenantId; // Azure AD Tenant ID
        private readonly string _clientSecret; // Azure AD App Client Secret

        public SharePointUploader(string clientId, string tenantId, string clientSecret, string siteId)
        {
            _clientId = clientId;
            _tenantId = tenantId;
            _clientSecret = clientSecret;
            _siteId = siteId;
            _graphClient = CreateGraphClient();
        }

        private GraphServiceClient CreateGraphClient()
        {
            // The client credentials flow requires that you request the
            // /.default scope, and pre-configure your permissions on the
            // app registration in Azure. An administrator must grant consent
            // to those permissions beforehand.
            var scopes = new[] { "https://graph.microsoft.com/.default" };


            // using Azure.Identity;
            var options = new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            };

            // https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var clientSecretCredential = new ClientSecretCredential(
                _tenantId, _clientId, _clientSecret, options);

            var graphClient = new GraphServiceClient(clientSecretCredential, scopes);
            return graphClient;
        }

        public async Task UploadFileAsync(string filePath, string destinationFolder)
        {
            try
            {
                // Read file content
                /*using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                string fileName = Path.GetFileName(filePath);

                // Get the drive (document library)
                var drive = await _graphClient.Sites[_siteId].Drive
                    .Request()
                    .GetAsync();

                // Upload file
                if (fileStream.Length <= 4 * 1024 * 1024) // 4MB threshold
                {
                    // Simple upload for small files
                    await _graphClient.Drives[drive.Id].Root
                        .ItemWithPath($"{destinationFolder}/{fileName}")
                        .Content
                        .Request()
                        .PutAsync<DriveItem>(fileStream);
                }
                else
                {
                    // Large file upload with resumable sessions
                    var uploadProps = new UploadSessionRequestOptions
                    {
                        Item = new DriveItem { Name = fileName }
                    };

                    var uploadSession = await _graphClient.Drives[drive.Id].Root
                        .ItemWithPath($"{destinationFolder}/{fileName}")
                        .CreateUploadSession()
                        .Request()
                        .PostAsync();

                    const int chunkSize = 320 * 1024; // 320 KB chunks
                    var largeFileUploader = new LargeFileUploadTask<DriveItem>(
                        uploadSession, fileStream, chunkSize);

                    await largeFileUploader.UploadAsync();
                }
                
                Console.WriteLine($"File {fileName} uploaded successfully");*/
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
                throw;
            }
        }
    }
    public class SharepointTaskParams : TaskParamsBase
    {
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string SiteId { get; set; } = string.Empty;
        public string SiteUrl { get; set; } = string.Empty;
        public string LibraryName { get; set; } = string.Empty;
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

        public override async Task Execute()
        {

            var uploader = new SharePointUploader(TaskParams.ClientId, TaskParams.TenantId, TaskParams.ClientSecret, TaskParams.SiteId);

            // Example upload
            string filePath = @"C:\Files\document.pdf";
            string destinationFolder = "Documents/Subfolder";

            await uploader.UploadFileAsync(filePath, destinationFolder);

            await Task.CompletedTask;
        }
    }
}
