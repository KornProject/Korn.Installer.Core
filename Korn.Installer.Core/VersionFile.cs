using Korn.Shared;
using System.IO;

namespace Korn.Installer.Core
{
    public class VersionFile
    {
        public VersionFile(string versionPath, string moduleName) => (this.versionPath, this.moduleName) = (versionPath, moduleName);

        readonly string versionPath;
        readonly string moduleName;

        public void SetVersion(string version) => File.WriteAllText(versionPath, version);
        public string GetVersion()
        {
            if (!File.Exists(versionPath))
            {
                KornShared.Logger.WriteWarning(
                    "Korn.Installer.Core.VersionFile->GetVersion: ",
                    $"Version file not found for module \"{moduleName}\"."
                );

                return "0";
            }

            var version = File.ReadAllText(versionPath);
            return version;
        }
    }
}