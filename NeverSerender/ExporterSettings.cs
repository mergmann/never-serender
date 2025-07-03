using System.Collections.Generic;
using VRageMath;

namespace NeverSerender
{
    public class ExporterSettings
    {
        public string LogPath { get; set; }
        public string OutPath { get; set; }
        public bool AutoFlush { get; set; }
        public bool ExportNonGrids { get; set; }
        public IList<long> ExportEntityIds { get; set; }
        public MatrixD ViewMatrix { get; set; } = MatrixD.Identity;
    }
}