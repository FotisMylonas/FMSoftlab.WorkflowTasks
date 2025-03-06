using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FMSoftlab.WorkflowTasks.Tasks
{
    public class FileZipperParams : TaskParamsBase
    {
        public CompressionLevel CompressionLevel { get; set; }
        public bool IncludeBaseDirectory { get; set; }
        public string SourceDirectory { get; set; }
        public string DestinationZipFile { get; set; }
        public override void LoadResults(IGlobalContext globalContext)
        {

        }
    }
    public class FileZipper : BaseTaskWithParams<FileZipperParams>
    {
        public FileZipper(string name, IGlobalContext globalContext, BaseTask parent, FileZipperParams taskParams, ILogger log) : base(name, globalContext, parent, taskParams, log)
        {
        }

        public FileZipper(string name, IGlobalContext globalContext, FileZipperParams taskParams, ILogger log) : base(name, globalContext, taskParams, log)
        {
        }

        private async Task<string> ZipDirectoryAsync(string sourceDirectory, string destinationZipFile)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                _log?.LogError("Source directory not found: {SourceDirectory}", sourceDirectory);
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectory}");
            }

            _log?.LogInformation("Starting compression of {SourceDirectory} to {DestinationZipFile}", sourceDirectory, destinationZipFile);

            await Task.Run(() => ZipFile.CreateFromDirectory(sourceDirectory, destinationZipFile, TaskParams.CompressionLevel, TaskParams.IncludeBaseDirectory));

            _log?.LogInformation("Compression completed: {DestinationZipFile}", destinationZipFile);

            string checksum = await ComputeFileChecksumAsync(destinationZipFile);
            _log?.LogInformation("Checksum (SHA-256) for {DestinationZipFile}: {Checksum}", destinationZipFile, checksum);

            return checksum;
        }

        private static async Task<string> ComputeFileChecksumAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
            byte[] hash = await sha256.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public override async Task Execute()
        {
            await ZipDirectoryAsync(TaskParams.SourceDirectory, TaskParams.DestinationZipFile);
        }
    }
}