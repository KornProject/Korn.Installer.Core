using System.Collections.Generic;

class ModuleInstaller
{
    public ModuleInstaller(params LocalGitDirectoryInstaller[] gitInstallers) => this.gitInstallers = gitInstallers;

    LocalGitDirectoryInstaller[] gitInstallers;

    public int Parts => gitInstallers.Length;

    public List<LocalGitDirectoryInstaller> GetOutdatedGitInstallers()
    {
        var result = new List<LocalGitDirectoryInstaller>();
        foreach (var installer in gitInstallers)
            if (installer.IsOutdated())
                result.Add(installer);

        return result;
    }
}