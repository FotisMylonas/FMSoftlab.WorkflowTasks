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
    public enum ZipAction { None, Zip, Unzip };

    public class FileZipperParams : TaskParamsBase
    {
        public ZipAction ZipAction { get; set; } = ZipAction.None;
        public CompressionLevel CompressionLevel { get; set; }
        public bool IncludeBaseDirectory { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
        public override void LoadResults(IGlobalContext globalContext)
        {

        }
    }
    public class UnzipProgress
    {
        public string FileName { get; set; } = string.Empty;
        public int Current { get; set; }
        public int Total { get; set; }
        public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;
    }
    public class FileZipper : BaseTaskWithParams<FileZipperParams>
    {
        public FileZipper(string name, IGlobalContext globalContext, BaseTask parent, FileZipperParams taskParams, ILogger log) : base(name, globalContext, parent, taskParams, log)
        {
        }

        public FileZipper(string name, IGlobalContext globalContext, FileZipperParams taskParams, ILogger log) : base(name, globalContext, taskParams, log)
        {
        }

        private async Task<string> ZipFolderWithProgressAsync(string sourceFolder, string zipFile, IProgress<double> progress)
        {
            string checksum = string.Empty;
            if (!Directory.Exists(sourceFolder))
            {
                _log?.LogError("Source directory not found: {SourceDirectory}", sourceFolder);
                throw new DirectoryNotFoundException($"Source directory not found: {sourceFolder}");
            }

            _log?.LogInformation("Starting compression of {sourceFolder} to {DestinationZipFile}", sourceFolder, zipFile);

            const int bufferSize = 81920; // 80KB buffer
            var allFiles = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);
            var allDirs = Directory.GetDirectories(sourceFolder, "*", new EnumerationOptions { RecurseSubdirectories=true });

            long totalBytes = allFiles.Sum(file => new FileInfo(file).Length);
            long bytesProcessed = 0;

            string basePath = TaskParams.IncludeBaseDirectory
                ? Path.GetDirectoryName(sourceFolder.TrimEnd(Path.DirectorySeparatorChar)) ?? string.Empty
                : sourceFolder;

            using (FileStream zipToOpen = new FileStream(zipFile, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create, leaveOpen: false))
            {
                // Add empty directories (ZipArchive requires a trailing slash to recognize folders)
                foreach (var dir in allDirs)
                {
                    var relativeDir = Path.GetRelativePath(basePath, dir).Replace('\\', '/') + '/';
                    /*if (!archive.Entries.Any(e => e.FullName.StartsWith(relativeDir, StringComparison.OrdinalIgnoreCase)))
                    {
                        archive.CreateEntry(relativeDir); // Empty entry for the folder
                    }*/
                    archive.CreateEntry(relativeDir);
                }

                // Add all files
                foreach (var file in allFiles)
                {
                    string relativePath = Path.GetRelativePath(basePath, file).Replace('\\', '/');
                    ZipArchiveEntry entry = archive.CreateEntry(relativePath, TaskParams.CompressionLevel);

                    using (FileStream fileToCompress = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true))
                    using (Stream entryStream = entry.Open())
                    {
                        byte[] buffer = new byte[bufferSize];
                        int bytesRead;
                        while ((bytesRead = await fileToCompress.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await entryStream.WriteAsync(buffer, 0, bytesRead);
                            bytesProcessed += bytesRead;
                            progress?.Report((double)bytesProcessed / totalBytes);
                        }
                    }
                }
            }

            checksum = await ComputeFileChecksumAsync(zipFile);
            _log?.LogInformation("Checksum (SHA-256) for {DestinationZipFile}: {Checksum}", zipFile, checksum);
            return checksum;
        }

        private static async Task<string> ComputeFileChecksumAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
            byte[] hash = await sha256.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private async Task UnzipAsync(
            string zipPath, 
            string extractPath, 
            IProgress<UnzipProgress> progress = null)
        {
            await Task.Run(() =>
            {
                using ZipArchive archive = ZipFile.OpenRead(zipPath);

                int totalEntries = archive.Entries.Count;
                int processedEntries = 0;

                foreach (var entry in archive.Entries)
                {
                    string destinationPath = Path.Combine(extractPath, entry.FullName);

                    // Ensure destination directory exists
                    string? destinationDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destinationDir))
                        Directory.CreateDirectory(destinationDir);

                    // Only extract files (not directories)
                    if (!string.IsNullOrEmpty(entry.Name))
                    {
                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }

                    processedEntries++;
                    progress?.Report(new UnzipProgress
                    {
                        FileName = entry.FullName,
                        Current = processedEntries,
                        Total = totalEntries
                    });
                }
            });
        }

        public override async Task Execute()
        {
            if (TaskParams.ZipAction==ZipAction.None)
            {
                _log.LogWarning("zip action not defined");
                return;
            }
            if (TaskParams.ZipAction==ZipAction.Zip)
            {
                if (!Path.Exists(TaskParams.Source))
                {
                    _log?.LogWarning("Source directory not found: {SourceDirectory}", TaskParams.Source);
                    return;
                }
                if (string.IsNullOrWhiteSpace(TaskParams.Destination))
                {
                    _log?.LogWarning("undefined DestinationZipFile");
                    return;
                }
                IProgress<double> progress = new Progress<double>(value =>
                {
                    _log?.LogInformation("Compression progress: {Progress:P}", value);
                });
                await ZipFolderWithProgressAsync(TaskParams.Source, TaskParams.Destination, progress);
            }
            if (TaskParams.ZipAction==ZipAction.Unzip)
            {
                if (!File.Exists(TaskParams.Source))
                {
                    _log?.LogWarning("Source zip file not found: {SourceZipFile}", TaskParams.Source);
                    return;
                }
                if (string.IsNullOrWhiteSpace(TaskParams.Destination))
                {
                    _log?.LogWarning("undefined Destination folder for unzipping");
                    return;
                }
                _log?.LogInformation("Unzipping {SourceZipFile} to {DestinationFolder}", TaskParams.Source, TaskParams.Destination);
                await UnzipAsync(TaskParams.Source, TaskParams.Destination, new Progress<UnzipProgress>(progress =>
                {
                    _log?.LogInformation("Unzipping progress: {FileName} ({Current}/{Total}) - {Percentage:P}", progress.FileName, progress.Current, progress.Total, progress.Percentage);
                }));
            }
        }
    }
}