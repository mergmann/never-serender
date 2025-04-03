using System.Collections.Generic;
using VRageMath;

namespace NeverSerender.Snapshot
{
    public struct GridSnapshot
    {
        public long EntityId { get; set; }
        public string Name { get; set; }
        public float Scale { get; set; }

        public IList<BlockSnapshot> Blocks { get; set; }
        public IList<Vector3I> RemovedBlocks { get; set; }
    }
}