using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.WorkflowTasks
{
    public class ExportCSVAzureBlobStorageParams : TaskParamsBase
    {
        public string ConnectionString { get; set; }
        public string Filename { get; set; }
        public string ContainerName { get; set; }

        public ExportCSVAzureBlobStorageParams()
        {

        }

        public override void LoadResults(IGlobalContext globalContext)
        {

        }
    }
    public class ExportCSVAzureBlobStorage : BaseTaskWithParams<ExportCSVAzureBlobStorageParams>
    {
        public ExportCSVAzureBlobStorage(string name, IGlobalContext globalContext, BaseTask parent, ExportCSVAzureBlobStorageParams taskParams, ILogger<ExportCSVAzureBlobStorage> log) : base(name, globalContext, parent, taskParams, log)
        {

        }
        public ExportCSVAzureBlobStorage(string name, IGlobalContext globalContext, ExportCSVAzureBlobStorageParams taskParams, ILogger<ExportCSVAzureBlobStorage> log) : base(name, globalContext, taskParams, log)
        {

        }
        public override async Task Execute()
        {
            await Task.CompletedTask;
            /*// Get the column names from the first row of the results

            StringBuilder csvBuilder = null;
            // Convert the StringBuilder to a byte array
            byte[] csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());

            // Create a CloudStorageAccount using the connection string
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(TaskParams.ConnectionString);

            // Create a CloudBlobClient using the storage account
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Get a reference to the container
            CloudBlobContainer container = blobClient.GetContainerReference(TaskParams.ContainerName);

            // Create the container if it doesn't already exist
            await container.CreateIfNotExistsAsync();

            // Get a reference to the blob
            CloudBlockBlob blob = container.GetBlockBlobReference(TaskParams.Filename);

            // Upload the CSV file to the blob
            using (Stream csvStream = new MemoryStream(csvBytes))
            {
                await blob.UploadFromStreamAsync(csvStream);
            }*/
        }
    }
}