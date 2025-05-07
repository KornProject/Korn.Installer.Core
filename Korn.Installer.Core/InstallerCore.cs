using Korn.Interface;
using System.Linq;
using System.IO;
using System.Collections.Generic;

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
        .Concat(Korn.Interface.Plugins.DefaultPluginNames.Select(name =>
        {
            var plugin = Interface.Plugins.GetDirectoryInfo(name);
            var entry = new GithubEntry(
                name,
                $"Binaries/Plugins/{name}",
                plugin.RootDirectory,
                plugin.VersionFilePath,
                Interface.Plugins.PluginManifestFileName
            );
            return entry;
        }
        ))
        .Select(entry => new LocalGitDirectoryInstaller(entry))
        .Select(installer => new ModuleInstaller(installer))
        .ToArray();

        static List<LocalGitDirectoryInstaller> outdatedGitInstallers;
        static List<LocalGitDirectoryInstaller> OutdatedGitInstallers
            => outdatedGitInstallers != null ? outdatedGitInstallers : outdatedGitInstallers = GetOutdatedGitInstallers();

        public static bool IsInstalled
        {
            get
            {
                var directories = Interface.KornDirectory.GetAllDirectories();
                foreach (var directory in directories)
                    if (!Directory.Exists(directory))
                        return false;

                return true;
            }
        }

        public static void DeleteDirectories() => Directory.Delete(Interface.KornDirectory.RootDirectory, true);

        public static void CreateDirectories()
        {
            var directories = Interface.KornDirectory.GetAllDirectories();
            foreach (var directory in directories)
                Directory.CreateDirectory(directory);
        }

        static List<LocalGitDirectoryInstaller> GetOutdatedGitInstallers() => installers.SelectMany(installer => installer.GetOutdatedGitInstallers()).ToList();

        public static void Install(InstallTrace trace) => Install(trace, OutdatedGitInstallers);

        static void Install(InstallTrace trace, List<LocalGitDirectoryInstaller> installers)
        {
            trace.Setup(installers.Count);

            foreach (var installer in installers)
                installer.Install(trace);
        }

        public static void CheckUpdates()
        {
            var installers = OutdatedGitInstallers.Where(installer => installer.Name != Interface.ServiceHub.GithubEntry.Name).ToList();
            var _ = new InstallTrace();
            Install(_, installers);
        }
    }
}