using VRageMath;

namespace NeverSerender.Output
{
    public struct EntityProperties
    {
        public long EntityId { get; set; }
        public uint? Parent { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public Matrix? LocalMatrix { get; set; }
        public MatrixD? WorldMatrix { get; set; }
        public Vector3UByte? Color { get; set; }
        public float Scale { get; set; }
        public bool? IsPreview { get; set; }
        public bool? Show { get; set; }
        public bool Remove { get; set; }
    }
}