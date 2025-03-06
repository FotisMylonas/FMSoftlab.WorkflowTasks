using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FMSoftlab.WorkflowTasks.Tasks
{
    public class CopyFolderParams : TaskParamsBase
    {
        public string SourceFolder { get; set; }
        public string DestinationFolder { get; set; }
        public Action<string, string> ProgressUpdated;
        public override void LoadResults(IGlobalContext globalContext)
        {

        }
    }
    public class CopyFolder : BaseTaskWithParams<CopyFolderParams>
    {
        public CopyFolder(string name, IGlobalContext globalContext, BaseTask parent, CopyFolderParams taskParams, ILogger log) : base(name, globalContext, parent, taskParams, log)
        {

        }

        public CopyFolder(string name, IGlobalContext globalContext, CopyFolderParams taskParams, ILogger log) : base(name, globalContext, taskParams, log)
        {

        }
        private async Task CopyFolderAsync(string sourcePath, string destinationPath)
        {
            if (!Directory.Exists(sourcePath))
                throw new DirectoryNotFoundException($"Source folder not found: {sourcePath}");

            Directory.CreateDirectory(destinationPath);
            await CopyFilesRecursivelyAsync(sourcePath, destinationPath);
        }

        private async Task CopyFilesRecursivelyAsync(string source, string destination)
        {
            foreach (var directory in Directory.GetDirectories(source))
            {
                string destDir = Path.Combine(destination, Path.GetFileName(directory));
                Directory.CreateDirectory(destDir);
                await CopyFilesRecursivelyAsync(directory, destDir);
            }

            var copyTasks = new List<Task>();
            foreach (var file in Directory.GetFiles(source))
            {
                string destFile = Path.Combine(destination, Path.GetFileName(file));
                copyTasks.Add(CopyFileWithVerificationAsync(file, destFile));
            }

            await Task.WhenAll(copyTasks);
        }

        private async Task CopyFileWithVerificationAsync(string sourceFile, string destinationFile)
        {
            const int maxRetries = 3;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                attempt++;
                await CopyFileAsync(sourceFile, destinationFile);

                if (await VerifyFileIntegrityAsync(sourceFile, destinationFile))
                {
                    _log?.LogDebug($"Copied: {sourceFile} -> {destinationFile}");
                    TaskParams.ProgressUpdated.Invoke(sourceFile, destinationFile);
                    return;
                }
                _log?.LogError($"Verification failed for {destinationFile}, retrying ({attempt}/{maxRetries})...");
            }
            _log?.LogError($"Failed to copy {sourceFile} after {maxRetries} attempts");
        }

        private async Task CopyFileAsync(string sourceFile, string destinationFile)
        {
            const int bufferSize = 81920;
            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true))
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
            }
            if (string.IsNullOrWhiteSpace(TaskParams.SourceFolder) || string.IsNullOrWhiteSpace(TaskParams.DestinationFolder))
            {
                _log?.LogWarning("CopyFiles, Source and destination folders must be specified");
            }
            await CopyFolderAsync(TaskParams.SourceFolder, TaskParams.DestinationFolder);
        }
    }
}
