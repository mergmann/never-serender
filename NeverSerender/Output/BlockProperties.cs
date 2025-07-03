using System.Collections.Generic;
using VRageMath;

namespace NeverSerender.Output
{
    public struct BlockProperties
    {
        public long? EntityId { get; set; }
        public uint GridId { get; set; }
        public string Name { get; set; }
        public bool Remove { get; set; }
        public Vector3S Position { get; set; }
        public Vector3 Translation { get; set; }
        public MatrixI Orientation { get; set; }
        public Vector3UByte Color { get; set; }
        public uint? Model { get; set; }
        public List<(uint, uint)> Modifiers { get; set; }
    }
}