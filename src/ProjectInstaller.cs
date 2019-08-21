using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AutoSubsync
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
            AfterInstall += SetDirectoryConfigurationEvent;
        }

        private void SetDirectoryConfigurationEvent(object sender, InstallEventArgs e)
        {
            var installUtilParams = this.Context.Parameters;
            if (!installUtilParams.ContainsKey("assemblypath"))
            {
                Console.WriteLine("Error during installation. Configuration must be performed manually.");
            }
            else
            {
                string assemblyPath = installUtilParams["assemblypath"];
                Console.WriteLine(assemblyPath);
                SetupDirectories(assemblyPath);
            }
            Console.WriteLine("The configuration can be performed manually in AutoSubsync.exe.config in the same folder as AutoSubsync.exe.");
        }

        private void SetupDirectories(string assemblyPath)
        {
            Console.WriteLine("This service will listen to certain directories (and sub-directories) for new .srt files. \r\n" +
                "When a new .srt file is detected, they will try to match the .srt file with a video file in the same folder, and run subsync on them. \r\n\r\n" +
                "Would you like to setup folders that should be listened to now?");
            List<string> directories;
            if (GetYesConfirmation())
                directories = GatherDirectoriesFromUser();
            else
                return;

            if (directories.Count == 0)
                return;

            string directorySetupString = GetDirectorySetupString(directories);

            SetDirectoryConfiguration(assemblyPath, directorySetupString);
        }

        private string SetupDirectory()
        {
            Console.WriteLine("Please input directory. Hit enter if done.");
            string directory = Console.ReadLine();
            if (directory == "")
                return "";
            if (!Directory.Exists(directory))
            {
                Console.WriteLine("Directory " + directory + " was not found. It should be entered in format like D:\\Downloads\\TV Shows.");
                return SetupDirectory();
            }
            return directory;
        }

        private void SetDirectoryConfiguration(string assemblyPath, string directorySetupString)
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(assemblyPath);
            Console.WriteLine(configFile.FilePath);
            var appSettings = configFile.AppSettings.Settings;
            if (appSettings["directory"] == null)
                appSettings.Add("directory", directorySetupString);
            else
                appSettings["directory"].Value = directorySetupString;
            configFile.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
        }

        private List<string> GatherDirectoriesFromUser()
        {
            List<string> directories = new List<string>();
            string directory = SetupDirectory();
            while (directory != "")
            {
                directories.Add(directory);
                directory = SetupDirectory();
            }
            return directories;
        }

        private string GetDirectorySetupString(List<string> directories)
        {
            string directorySetupString;
            if (directories.Count == 0)
                directorySetupString = "Input directories to listen to here (semi-colon separated if more than one).";
            else
                directorySetupString = directories.Aggregate((string acc, string newDirectory) => acc + ";" + newDirectory);
            return directorySetupString;
        }

        private bool GetYesConfirmation()
        {
            Console.WriteLine("(y/n)");
            char reply = (char)Console.ReadKey().KeyChar;
            Console.WriteLine();
            if (reply == 'y' || reply == 'Y')
                return true;
            else if (reply == 'n' || reply == 'N')
                return false;
            else
            {
                Console.WriteLine("Please answer y or n (yes or no)");
                return GetYesConfirmation();
            }
        }
    }
}
