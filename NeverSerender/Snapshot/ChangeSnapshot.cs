using System.Collections.Generic;

namespace NeverSerender.Snapshot
{
    public class ChangeSnapshot
    {
        public float? Advance { get; set; }
        public IList<EntitySnapshot> Entities { get; set; }
        public IList<EntitySnapshot> EntitiesDelayed { get; set; }
        public IList<GridSnapshot> Grids { get; set; }
        public IList<LightSnapshot> Lights { get; set; }
    }
}