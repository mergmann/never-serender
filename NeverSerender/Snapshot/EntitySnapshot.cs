using VRage.Game.Entity;
using VRageMath;

namespace NeverSerender.Snapshot
{
    public class EntitySnapshot
    {
        public EntitySnapshot()
        {
        }

        public EntitySnapshot(MyEntity entity)
        {
            EntityId = entity.EntityId;
            Parent = entity.Parent?.EntityId;
            Name = entity.DisplayName;
            Model = entity.Model?.AssetName;
            Color = entity.Render?.ColorMaskHsv;
            IsPreview = entity.IsPreview;
            Show = entity.InScene;
            Remove = false;

            if (entity.Parent != null)
            {
                LocalMatrix = entity.PositionComp.LocalMatrixRef;
                WorldMatrix = null;
            }
            else
            {
                LocalMatrix = null;
                WorldMatrix = entity.PositionComp.WorldMatrixRef;
            }
        }

        public long EntityId { get; set; }
        public long? Parent { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public Matrix? LocalMatrix { get; set; }
        public MatrixD? WorldMatrix { get; set; }
        public Vector3? Color { get; set; }
        public bool? IsPreview { get; set; }
        public bool? Show { get; set; }
        public bool Remove { get; set; }
    }
}