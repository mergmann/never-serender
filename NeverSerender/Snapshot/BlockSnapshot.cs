using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using VRageMath;

namespace NeverSerender.Snapshot
{
    public struct BlockSnapshot
    {
        public BlockSnapshot(MySlimBlock block)
        {
            block.GetLocalMatrix(out var localMatrix);

            EntityId = block.FatBlock?.EntityId;
            Model = Util.GetActiveBlockModel(block, out var orientation);
            Skin = block.SkinSubtypeId.String;
            Color = block.ColorMaskHSV;
            Position = block.Position;
            Translation = localMatrix.Translation;
            Orientation = orientation;
            Definition = block.BlockDefinition;
        }
        
        public long? EntityId { get; private set; }
        public string Model { get; private set; }
        public string Skin { get; private set; }
        public Vector3 Color { get; private set; }
        public Vector3I Position { get; private set; }
        public Vector3 Translation { get; private set; }
        public MatrixI Orientation { get; private set; }
        public MyCubeBlockDefinition Definition { get; private set; }
    }
}