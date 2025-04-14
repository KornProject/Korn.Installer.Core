class ModuleInstaller
{
    public ModuleInstaller(params LocalGitDirectoryInstaller[] gitInstallers) => this.gitInstallers = gitInstallers;

    LocalGitDirectoryInstaller[] gitInstallers;

    public void Install() 
    {
        foreach (var installer in gitInstallers)
            installer.Install();
    }

    public void CheckUpdates()
    {
        foreach (var installer in gitInstallers)
            installer.CheckUpdates();
    }
}