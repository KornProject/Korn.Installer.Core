using System.Collections.Generic;
using Korn.Utils.GithubExplorer;
using Korn.Interface;
using System.IO;
using Korn;

class LocalGitDirectoryInstaller
{            
    static RepositoryID OverviewRepository = new RepositoryID("YoticKorn", "Overview");

    public LocalGitDirectoryInstaller(GithubEntry entry) : this(entry.GithubPath, entry.DirectoryPath, entry.VersionFilePath, entry.VersionableFileName) { }

    public LocalGitDirectoryInstaller(string githubPath, string directoryPath, string versionFilePath, string versionableFileName)
    {
        (this.githubPath, this.directoryPath, this.versionableFileName) = (githubPath, directoryPath, versionableFileName);

        versionFile = new VersionFile(versionFilePath, $"{GetType().FullName}");
    }

    string githubPath, directoryPath, versionableFileName;
    VersionFile versionFile;

    List<RepositoryEntryJson> GetEntries() => GithubClient.Instance.GetRepositoryEntries(OverviewRepository, githubPath);

    public void Install()
    {
        var entries = GetEntries();
        Install(entries);
    }

    public void Install(List<RepositoryEntryJson> entries)
    {
        foreach (var entry in entries)
        {
            var fileName = entry.Name;
            var bytes = GithubClient.Instance.DownloadAsset(entry);
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
        var versionableEntry = entries.Find(e => e.Name == versionableFileName);
        if (versionableEntry == null)
            throw new KornError(
                "Korn.Installer.Core.InstallerCore.LocalGitDirectoryInstaller->CheckUpdates: ",
                "Can't find a versionable entry in the github overview directory.",
                "This could be due to problems uploading files to github or git failures."
            );

        return versionableEntry.Sha;
    }
}