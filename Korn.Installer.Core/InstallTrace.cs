using System;
using System.Runtime.InteropServices;

namespace Korn.Installer.Core
{
    public unsafe class InstallTrace
    {
        public Action<string> Notify;
        public int PartsCount;
        public int CurrentPartIndex = 0;
        public Part CurrentPart;

        public void Setup(int partsCount)
        {
            PartsCount = partsCount;
        }

        public void SetPart(Part part)
        {
            CurrentPart = part;
            CurrentPartIndex++;
            UpdateTrace();
        }

        public void AddDownloadedBytes(long downloaded) => SetDownloadedBytes(CurrentPart.DownloadedBytes.Bytes + downloaded);

        public void SetDownloadedBytes(long downloaded)
        {
            CurrentPart.DownloadedBytes = downloaded;
            UpdateTrace();
        }

        void UpdateTrace()
        {
            var part = CurrentPart;
            if (part is null)
                return;

            var trace = $"installing {part.Name}[{CurrentPartIndex}/{PartsCount}]: {(int)part.DownloadedBytes.KBytes} of {(int)part.TotalBytes.KBytes}kb";
            Notify?.Invoke(trace);
        }

        public class Part
        {
            public Part(string name, long totalBytes) => (Name, TotalBytes) = (name, totalBytes);

            public readonly string Name;
            public readonly DataNumber TotalBytes;

            public DataNumber DownloadedBytes;
        }

        [StructLayout(LayoutKind.Sequential, Size = sizeof(long))]
        public struct DataNumber
        {
            public long Bytes;
            public double KBytes => Bytes / 1024;
            public double MBytes => KBytes / 1024;
            public double GBytes => MBytes / 1024;

            public static implicit operator DataNumber(long bytes) => *(DataNumber*)&bytes;
        }
    }
}