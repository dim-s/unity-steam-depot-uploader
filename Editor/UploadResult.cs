using System;

namespace Plugins.GalacticWorkshop.SteamDepotUploader.Editor
{
    public class UploadResult
    {
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public string UploadId { get; set; }
        public string BuildId { get; set; }
        public string AppId { get; set; }
        public string DepotId { get; set; }
        public string BuildPath { get; set; }
        public DateTime UploadTime { get; set; }
        public string LogOutput { get; set; }
    }
}