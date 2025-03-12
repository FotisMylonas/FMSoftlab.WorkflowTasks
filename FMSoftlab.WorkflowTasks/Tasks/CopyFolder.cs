using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.IO;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace FMSoftlab.WorkflowTasks.Tasks
{
    public class CopyFolderParams : TaskParamsBase
    {
        public int MaxDegreeOfParallelism { get; set; } = 5;
        public string SourceFolder { get; set; }
        public string DestinationFolder { get; set; }
        public Func<string, string, Task> ProgressUpdated;
        public override void LoadResults(IGlobalContext globalContext)
        {

        }
    }
    public class CopyFolder : BaseTaskWithParams<CopyFolderParams>
    {
        class FileCopyInfo
        {
            public string SourceFile { get; set; }
            public string DestinationFolder { get; set; }
        }
        public CopyFolder(string name, IGlobalContext globalContext, BaseTask parent, CopyFolderParams taskParams, ILogger log) : base(name, globalContext, parent, taskParams, log)
        {

        }

        public CopyFolder(string name, IGlobalContext globalContext, CopyFolderParams taskParams, ILogger log) : base(name, globalContext, taskParams, log)
        {

        }

        private async Task CopyFilesRecursivelyAsync(string source, string destination)
        {
            // Create destination directory if it doesn't exist
            source = Path.GetFullPath(source);
            destination = Path.GetFullPath(destination);
            _log?.LogDebug($"Copying {source} to {destination}");
            Directory.CreateDirectory(destination);

            // Process directories recursively
            foreach (var directory in Directory.GetDirectories(source))
            {
                string destDir = Path.Combine(destination, Path.GetFileName(directory));
                await CopyFilesRecursivelyAsync(directory, destDir);
            }

            ActionBlock<FileCopyInfo> _copyFileBlock = new ActionBlock<FileCopyInfo>(async fileCopy =>
            {
                string destFile = Path.Combine(fileCopy.DestinationFolder, Path.GetFileName(fileCopy.SourceFile));
                await CopyFileWithVerificationAsync(fileCopy.SourceFile, destFile);
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = TaskParams.MaxDegreeOfParallelism // Set the maximum degree of parallelism
            });

            // Process files in the current directory
            foreach (var file in Directory.GetFiles(source))
            {
                // Post the file path to the ActionBlock for processing
                _copyFileBlock.Post(new FileCopyInfo { SourceFile= file, DestinationFolder=destination });
            }

            // Signal that no more items will be posted to the block
            _copyFileBlock.Complete();

            // Wait for all file copy operations to complete
            await _copyFileBlock.Completion;
        }

        private async Task CopyFileWithVerificationAsync(string sourceFile, string destinationFile)
        {
            const int maxRetries = 3;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                attempt++;
                try
                {
                    await CopyFileAsync(sourceFile, destinationFile);
                    if (await VerifyFileIntegrityAsync(sourceFile, destinationFile))
                    {
                        _log?.LogDebug($"Verified copy: {sourceFile} -> {destinationFile}");
                        if (TaskParams.ProgressUpdated!=null)
                        {
                            await TaskParams.ProgressUpdated(sourceFile, destinationFile);
                        }
                        return;
                    }
                    else
                    {
                        _log?.LogError($"Verification failed for {destinationFile}, retrying ({attempt}/{maxRetries})...");
                    }
                }
                catch (IOException ex)
                {
                    _log?.LogError(ex, $"IO error copying {sourceFile} to {destinationFile}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    _log?.LogError(ex, $"Access denied copying {sourceFile} to {destinationFile}");
                    //throw; // Re-throw as this is unlikely to be resolved by retrying
                }
                catch (Exception ex)
                {
                    _log?.LogError(ex, $"Unexpected error copying {sourceFile} to {destinationFile}");
                    //throw; // Re-throw unexpected exceptions
                }
            }
            _log?.LogError($"Failed to copy {sourceFile} after {maxRetries} attempts");
        }

        private async Task CopyFileAsync(string sourceFile, string destinationFile)
        {
            const int bufferSize = 81920;
            _log.LogDebug("Copying {sourceFile} to {destinationFile}", sourceFile, destinationFile);
            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, true))
            using (var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true))
            {
                await sourceStream.CopyToAsync(destinationStream);
            }
        }

        private async Task<bool> VerifyFileIntegrityAsync(string sourceFile, string destinationFile)
        {
            if (!File.Exists(destinationFile)) return false;

            FileInfo sourceInfo = new FileInfo(sourceFile);
            FileInfo destInfo = new FileInfo(destinationFile);
            if (sourceInfo.Length != destInfo.Length) return false;

            return await CompareFileHashesAsync(sourceFile, destinationFile);
        }

        private async Task<bool> CompareFileHashesAsync(string file1, string file2)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash1 = await ComputeFileHashAsync(md5, file1);
                byte[] hash2 = await ComputeFileHashAsync(md5, file2);
                return StructuralComparisons.StructuralEqualityComparer.Equals(hash1, hash2);
            }
        }

        private async Task<byte[]> ComputeFileHashAsync(HashAlgorithm hashAlgorithm, string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
            {
                return await Task.Run(() => hashAlgorithm.ComputeHash(stream));
            }
        }


        public override async Task Execute()
        {
            if (TaskParams is null)
            {
                _log?.LogWarning("CopyFiles, No params defined");
                return;
            }
            if (string.IsNullOrWhiteSpace(TaskParams.SourceFolder) || string.IsNullOrWhiteSpace(TaskParams.DestinationFolder))
            {
                _log?.LogWarning("CopyFiles, Source and destination folders must be specified");
                return;
            }
            if (!Directory.Exists(TaskParams.SourceFolder))
            {
                _log.LogWarning("Source folder not found: {SourceFolder}", TaskParams.SourceFolder);
                return;
            }
            await CopyFilesRecursivelyAsync(TaskParams.SourceFolder, TaskParams.DestinationFolder);
        }
    }
}
