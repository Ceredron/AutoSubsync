using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.ServiceProcess;

namespace AutoSubsync
{
    public partial class SubsyncService : ServiceBase
    {
        private static readonly string[] allowedVideoFileExtensions = { ".mp4", ".avi", ".mkv" }; // Probably also works with others
        private List<FileSystemWatcher> watchers;
        public SubsyncService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            watchers = new List<FileSystemWatcher>();
            RegisterFileHandler();
        }

        protected override void OnStop()
        {
            this.Dispose();
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")] // TODO, check if needed
        private void RegisterFileHandler()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string directoryArgument = appSettings.Get("directory");

            if (directoryArgument == null)
                throw new ConfigurationErrorsException("No directories in configuration file (AutoSubsync.exe.config <appSettings>).");

            string[] directories = directoryArgument.Split(';');

            foreach (string directory in directories) { 
                // Create a new FileSystemWatcher and set its properties.
                var watcher = new FileSystemWatcher();
                if (!System.IO.Directory.Exists(directory))
                    throw new DirectoryNotFoundException(directory + " is not a valid directory.");
                watcher.Path = @directory;
                watcher.IncludeSubdirectories = true;

                // Watch for changes in LastAccess and LastWrite times, and
                // the renaming of files or directories.
                watcher.NotifyFilter = NotifyFilters.FileName
                                        | NotifyFilters.DirectoryName;

                // Only watch srt files.
                watcher.Filter = "*.srt";

                // Add event handlers.
                watcher.Created += OnNewSubtitle;

                // Begin watching.
                watcher.EnableRaisingEvents = true;
                watchers.Add(watcher);
                WriteToEventLog("Now listening for subtitles in " + directory);
            }
        }

        private void OnNewSubtitle(object source, FileSystemEventArgs e)
        {
            string srtFileWithPath = e.FullPath;
            WriteToEventLog("New subtitle detected: " + srtFileWithPath);

            // Get list of all video files where new subtitle was saved
            string directoryPath = e.FullPath.Substring(0, e.FullPath.LastIndexOf('\\'));
            string[] filesInDirectory = Directory.GetFiles(directoryPath);
            // Search pattern parameter in GetFiles does not support multiple file extensions, next lines filters on valid file extensions
            filesInDirectory = filesInDirectory.Where<string>(file => allowedVideoFileExtensions.Any(file.ToLower().EndsWith)).ToArray();

            // Find the video file whose name matches the SRT file
            string srtFileName = srtFileWithPath.Substring(e.FullPath.LastIndexOf('\\') + 1);
            string srtFileNameWithoutExtension = srtFileName.Substring(0, srtFileName.LastIndexOf('.'));
            filesInDirectory = filesInDirectory.Where<string>(file => file.Contains(srtFileNameWithoutExtension)).ToArray();
            WriteToEventLog("Now looking for " + srtFileNameWithoutExtension + " video files.");

            if (filesInDirectory.Length == 1)
                SubsyncSubtitle(filesInDirectory[0], srtFileWithPath);
            else if (filesInDirectory.Length == 0)
                WriteToEventLog("Found no matching video file for " + srtFileWithPath);
            else
                WriteToEventLog("Found several matching video files for " + srtFileWithPath + ". Performed no sync.");
        }

        private void SubsyncSubtitle(string videoFilePath, string subtitleFilePath)
        {
            Console.WriteLine(subtitleFilePath);
            Console.WriteLine(videoFilePath);
            Process SubsyncProcess = new Process();
            SubsyncProcess.StartInfo.FileName = "subsync "
                + "\"" + videoFilePath + "\""
                + " -i "
                + "\"" + subtitleFilePath + "\""
                + " -o "
                + "\"" + subtitleFilePath + "\"";
            SubsyncProcess.StartInfo.UseShellExecute = false;
            SubsyncProcess.StartInfo.RedirectStandardOutput = true;
            SubsyncProcess.StartInfo.CreateNoWindow = true;
            SubsyncProcess.Start();

            while (!SubsyncProcess.StandardOutput.EndOfStream)
            {
                Console.WriteLine(SubsyncProcess.StandardOutput.ReadLine());
            }

            WriteToEventLog("Subsync process ended with exit code " + SubsyncProcess.ExitCode);
        }

        private void WriteToEventLog(string message)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = this.ServiceName;
                eventLog.BeginInit();
                eventLog.WriteEntry(message, EventLogEntryType.Information);
            }
        }
    }
}
