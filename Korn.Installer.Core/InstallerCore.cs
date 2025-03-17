using Korn.Interface.ServiceModule;
using System.Collections.Generic;
using Korn.Utils.GithubExplorer;
using Korn.Shared;
using System.IO;
using System;

namespace Korn.Installer.Core
{
    public static class InstallerCore
    {
        static readonly GithubClient GithubClient = new GithubClient();
        static readonly RepositoryID
            ServiceRepository = new RepositoryID("YoticKorn", "Korn.Service"),
            AutorunServiceRepository = new RepositoryID("YoticKorn", "Korn.AutorunService"),
            OverviewRepository = new RepositoryID("YoticKorn", "Overview");

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
            InstallerCore.Service.CheckUpdates();
            InstallerCore.Libraries.CheckUpdates();
            InstallerCore.Bootstrapper.CheckUpdates();
        }

        static string GetVersionFromRelease(RepositoryReleaseJson release) => Convert.ToString(release.CreatedAt.ToBinary(), 16);

        static string DownloadFromGithub(RepositoryID repository, string directoryPath)
        {
            var releases = GithubClient.GetRepositoryReleases(repository);
            var latestRelease = GetLatestRelease(repository, releases);
            return DownloadFromGithub(latestRelease, directoryPath);
        }

        static RepositoryReleaseJson GetLatestRelease(RepositoryID repository, List<RepositoryReleaseJson> releases)
        {
            if (releases.Count == 0)
                throw new KornError(
                    "Korn.Installer.ServiceInstaller->Install->DownloadFromGithub: ",
                    $"Repository {repository} doesn't has releases."
                );

            var latestRelease = releases[0];
            return latestRelease;
        }

        static string DownloadFromGithub(RepositoryReleaseJson release, string directoryPath)
        {
            var entries = release.Assets;
            for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
            {
                var entry = entries[entryIndex];
                var bytes = GithubClient.DownloadAsset(entry);

                var entryPath = Path.Combine(directoryPath, entry.Name);
                File.WriteAllBytes(entryPath, bytes);
            }

            return GetVersionFromRelease(release);
        }

        public static class Service
        {
            public static readonly VersionFile VersionFile = new VersionFile(Korn.Interface.AutorunService.VersionFile, "AutorunService");
            public static void Install()
            {
                DeleteBinaries();
                var version = DownloadFromGithub(ServiceRepository, Korn.Interface.ServiceModule.Service.BinNet8Directory);
                VersionFile.SetVersion(version);
            }

            static void DeleteBinaries()
            {
                foreach (var file in Directory.GetFiles(Korn.Interface.ServiceModule.Service.BinNet8Directory))
                    File.Delete(file);
            }

            public static void CheckUpdates()
            {
                var releases = GithubClient.GetRepositoryReleases(ServiceRepository);
                var latestRelease = GetLatestRelease(ServiceRepository, releases);
                var latestVersion = GetVersionFromRelease(latestRelease);
                var currentVersion = VersionFile.GetVersion();
                if (latestVersion != currentVersion)
                    Install();
            }
        }

        // cannot be updated
        public static class AutorunService
        {
            public static readonly VersionFile VersionFile = new VersionFile(Korn.Interface.AutorunService.VersionFile, "AutorunService");

            static void DeleteBinaries()
            {
                foreach (var file in Directory.GetFiles(Korn.Interface.AutorunService.BinNet472Diretory))
                    File.Delete(file);
            }

            public static void Install()
            {
                DeleteBinaries();
                var version = DownloadFromGithub(AutorunServiceRepository, Korn.Interface.AutorunService.BinNet472Diretory);
                VersionFile.SetVersion(version);
            }
        }

        public static class Bootstrapper
        {
            static readonly Net8 net8 = new Net8();
            static readonly Net472 net472 = new Net472();

            public static void Install()
            {
                net8.Install();
                net472.Install();
            }

            public static void CheckUpdates()
            {
                net8.CheckUpdates();
                net472.CheckUpdates();
            }

            class Net8 : LocalInstaller
            {
                public Net8()
                    : base(
                          "Binaries/Bootstrapper/net8",
                          Korn.Interface.Bootstrapper.BinNet8Directory,
                          Korn.Interface.Bootstrapper.Net8VersionFile
                    )
                { }
            }

            class Net472 : LocalInstaller
            {
                public Net472()
                    : base(
                          "Binaries/Bootstrapper/net472",
                          Korn.Interface.Bootstrapper.BinNet472Directory,
                          Korn.Interface.Bootstrapper.Net472VersionFile
                    ) { }
            }

            class LocalInstaller
            {
                public LocalInstaller(string githubPath, string directoryPath, string versionFilePath)
                {
                    (this.githubPath, this.directoryPath, this.versionFilePath) = (githubPath, directoryPath, versionFilePath);

                    versionFile = new VersionFile(versionFilePath, $"Bootstrapper({GetType().Name})");
                }

                readonly string githubPath, directoryPath, versionFilePath;
                readonly VersionFile versionFile;

                List<RepositoryEntryJson> GetEntries() => GithubClient.GetRepositoryEntries(OverviewRepository, githubPath);

                public void Install()
                {
                    var entries = GetEntries();
                    Install(entries);
                }

                public void Install(List<RepositoryEntryJson> entries)
                {
                    foreach (var entry in entries)
                        if (entry.Name != "plug")
                        {
                            var fileName = entry.Name;
                            var bytes = GithubClient.DownloadAsset(entry);
                            var filePath = Path.Combine(directoryPath, fileName);
                            File.WriteAllBytes(filePath, bytes);
                        }

                    var version = GetVersionFromEntries(entries);
                    versionFile.SetVersion(version);
                }

                public void CheckUpdates()
                {
                    var entries = GetEntries();

                    var currentVersion = versionFile.GetVersion();
                    var newVersion = GetVersionFromEntries(entries);

                    if (currentVersion == newVersion)
                        return;

                    Install(entries);
                }

                string GetVersionFromEntries(List<RepositoryEntryJson> entries)
                {
                    var versionableFileName = Korn.Interface.Bootstrapper.VersionableFileName;
                    var versionableEntry = entries.Find(e => e.Name == versionableFileName);
                    if (versionableEntry == null)
                        throw new KornError(
                            "Korn.Installer.Core.InstallerCore.Bootstrapper->CheckUpdates: ",
                            "Can't find a versinable entry in the github overview bootstrapper directory. ",
                            "This could be due to problems uploading files to github or git failures."
                        );

                    return versionableEntry.Sha;
                }
            }
        }

        public static class Libraries
        {
            public static void Install() => CheckUpdates();

            public static void CheckUpdates()
            {
                var hasLibrariesList = Korn.Interface.ServiceModule.Libraries.HasLibrariesList();
                var oldLibrariesList = hasLibrariesList ? Korn.Interface.ServiceModule.Libraries.DeserializeLibrariesList() : new LibrariesList();
                var oldLibraries = oldLibrariesList.Libraries;
                var librariesList = new LibrariesList();
                var libraries = librariesList.Libraries;

                var librariesDirectory = Korn.Interface.ServiceModule.Libraries.LibrariesDirectory;
                var net8LibrariesDirectory = Path.Combine(librariesDirectory, KornShared.Net8TargetVersion);
                var net472LibrariesDirectory = Path.Combine(librariesDirectory, KornShared.Net472TargetVersion);

                DownloadFiles($"Binaries/Libraries/{KornShared.Net8TargetVersion}", net8LibrariesDirectory, KornShared.Net8TargetVersion);
                DownloadFiles($"Binaries/Libraries/{KornShared.Net472TargetVersion}", net472LibrariesDirectory, KornShared.Net472TargetVersion);

                DownloadFiles($"Binaries/ExternalLibraries/{KornShared.Net8TargetVersion}", net8LibrariesDirectory, KornShared.Net8TargetVersion);
                DownloadFiles($"Binaries/ExternalLibraries/{KornShared.Net472TargetVersion}", net472LibrariesDirectory, KornShared.Net472TargetVersion);

                librariesList.Save();

                void DownloadFiles(string fromGithubPath, string toDirectoryPath, string targetVersion)
                {
                    var entries = GithubClient.GetRepositoryEntries(OverviewRepository, fromGithubPath);
                    foreach (var entry in entries)
                    {
                        var fileName = entry.Name;
                        if (fileName == "plug")
                            continue;

                        var name = Path.GetFileNameWithoutExtension(fileName);

                        var oldLibrary = GetOldLibrary(name);
                        if (oldLibrary != null && oldLibrary.CurrentEntrySha == entry.Sha)
                        {
                            libraries.Add(oldLibrary);
                            continue;
                        }

                        if (Path.GetExtension(fileName) == ".dll")
                        {
                            var library = new LibrariesList.Library()
                            {
                                Name = name,
                                CurrentEntrySha = entry.Sha,
                                TargetVersion = targetVersion
                            };

                            var hasLocalPath = oldLibrary != null && !string.IsNullOrEmpty(oldLibrary.LocalFilePath);
                            if (hasLocalPath)
                                library.LocalFilePath = oldLibrary.LocalFilePath;

                            libraries.Add(library);
                        }

                        var bytes = GithubClient.DownloadAsset(entry);
                        var filePath = Path.Combine(toDirectoryPath, fileName);
                        File.WriteAllBytes(filePath, bytes);
                    }

                    LibrariesList.Library GetOldLibrary(string name)
                    {
                        var entry = oldLibraries.Find(e => e.Name == name && e.TargetVersion == targetVersion);
                        return entry;
                    }
                }
            }
        }

        public static class Plugins
        {
            public static void Install() => CheckUpdates();

            public static void CheckUpdates()
            {
                var hasLibrariesList = Korn.Interface.ServiceModule.Libraries.HasLibrariesList();
                var oldLibrariesList = hasLibrariesList ? Korn.Interface.ServiceModule.Libraries.DeserializeLibrariesList() : new LibrariesList();
            }
        }
    }
}