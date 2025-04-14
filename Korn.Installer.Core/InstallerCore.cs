using Korn.Utils.GithubExplorer;
using Korn.Interface;
using Korn.Shared;
using System.Linq;
using System.IO;

namespace Korn.Installer.Core
{
    public static class InstallerCore
    {
        static ModuleInstaller[] installers = new GithubEntry[]
        {
            Interface.ServiceHub.GithubEntry,
            Interface.Bootstrapper.GithubNet8Entry,
            Interface.Bootstrapper.GithubNet472Entry,
            Interface.LoggerService.GithubEntry,
            Interface.InjectorService.GithubEntry
        }
        .Select(entry => new LocalGitDirectoryInstaller(entry))
        .Select(installer => new ModuleInstaller(installer))
        .ToArray();

        public static bool IsInstalled
        {
            get
            {
                var directories = Korn.Interface.KornDirectory.GetAllDirectories();
                foreach (var directory in directories)
                    if (!Directory.Exists(directory))
                        return false;

                return true;
            }
        }

        public static void DeleteDirectories() => Directory.Delete(Korn.Interface.KornDirectory.RootDirectory, true);

        public static void CreateDirectories()
        {
            var directories = Korn.Interface.KornDirectory.GetAllDirectories();
            foreach (var directory in directories)
                Directory.CreateDirectory(directory);
        }        

        public static void CheckUpdates()
        {
            foreach (var installer in installers)
                installer.CheckUpdates();
        }

        public static void Install()
        {
            foreach (var installer in installers)
                installer.Install();
        }

        
        public static class Libraries
        {
            public static void Install() => CheckUpdates();

            public static void CheckUpdates()
            {
                var hasLibrariesList = Korn.Interface.Libraries.HasLibrariesList();
                var oldLibrariesList = hasLibrariesList ? Korn.Interface.Libraries.DeserializeLibrariesList() : new LibrariesList();
                var oldLibraries = oldLibrariesList.Libraries;
                var librariesList = new LibrariesList();
                var libraries = librariesList.Libraries;

                var librariesDirectory = Korn.Interface.Libraries.LibrariesDirectory;
                var net8LibrariesDirectory = Path.Combine(librariesDirectory, KornShared.Net8TargetVersion);
                var net472LibrariesDirectory = Path.Combine(librariesDirectory, KornShared.Net472TargetVersion);

                DownloadFiles($"Binaries/Libraries/{KornShared.Net8TargetVersion}", net8LibrariesDirectory, KornShared.Net8TargetVersion);
                DownloadFiles($"Binaries/Libraries/{KornShared.Net472TargetVersion}", net472LibrariesDirectory, KornShared.Net472TargetVersion);

                DownloadFiles($"Binaries/ExternalLibraries/{KornShared.Net8TargetVersion}", net8LibrariesDirectory, KornShared.Net8TargetVersion);
                DownloadFiles($"Binaries/ExternalLibraries/{KornShared.Net472TargetVersion}", net472LibrariesDirectory, KornShared.Net472TargetVersion);

                librariesList.Save();

                void DownloadFiles(string fromGithubPath, string toDirectoryPath, string targetVersion)
                {
                    var client = GithubClient.Instance;
                    var overviewRepository = new RepositoryID("YoticKorn", "Overview");
                    var entries = client.GetRepositoryEntries(overviewRepository, fromGithubPath);
                    foreach (var entry in entries)
                    {
                        var fileName = entry.Name;
                        if (fileName == "plug")
                            continue;

                        var name = Path.GetFileNameWithoutExtension(fileName);

                        var oldLibrary = GetOldLibrary(name);
                        if (oldLibrary != null && oldLibrary.Sha == entry.Sha)
                        {
                            libraries.Add(oldLibrary);
                            continue;
                        }

                        if (Path.GetExtension(fileName) == ".dll")
                        {
                            var library = new LibrariesList.Library()
                            {
                                Name = name,
                                Sha = entry.Sha,
                                TargetVersion = targetVersion
                            };

                            var hasLocalPath = oldLibrary != null && !string.IsNullOrEmpty(oldLibrary.LocalFilePath);
                            if (hasLocalPath)
                                library.LocalFilePath = oldLibrary.LocalFilePath;

                            libraries.Add(library);
                        }

                        var bytes = client.DownloadAsset(entry);
                        var filePath = Path.Combine(toDirectoryPath, fileName);
                        File.WriteAllBytes(filePath, bytes);
                    }

                    LibrariesList.Library GetOldLibrary(string name) => oldLibraries.Find(e => e.Name == name && e.TargetVersion == targetVersion);
                }
            }
        }

        public static class Plugins
        {
            public static void Install() => CheckUpdates();

            public static void CheckUpdates()
            {
                var hasLibrariesList = Korn.Interface.Libraries.HasLibrariesList();
                var oldLibrariesList = hasLibrariesList ? Korn.Interface.Libraries.DeserializeLibrariesList() : new LibrariesList();
            }
        }
    }
}