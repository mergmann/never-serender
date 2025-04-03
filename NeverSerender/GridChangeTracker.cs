using System;
using System.Collections.Generic;
using System.Linq;
using NeverSerender.Snapshot;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using VRage.Game.Entity;
using VRageMath;

namespace NeverSerender
{
    public class GridChangeTracker : IDisposable
    {
        private readonly MyCubeGrid grid;

        private readonly object @lock = new object();
        private readonly EntityChangeTracker trackedEntities;
        private Dictionary<Vector3I, BlockSnapshot?> blocks = new Dictionary<Vector3I, BlockSnapshot?>();

        public GridChangeTracker(EntityChangeTracker trackedEntities, MyCubeGrid grid)
        {
            this.trackedEntities = trackedEntities;
            this.grid = grid;
            grid.OnBlockAdded += Add;
            grid.OnBlockRemoved += Remove;
            
            grid.GetBlocks().ForEach(Add);
        }

        public GridSnapshot Next()
        {
            var last = blocks;
            lock (@lock)
                blocks = new Dictionary<Vector3I, BlockSnapshot?>();
            
            var changedBlocks = new List<BlockSnapshot>();
            var removedBlocks = new List<Vector3I>();
            
            foreach (var pair in last)
            {
                if (pair.Value.HasValue)
                    changedBlocks.Add(pair.Value.Value);
                else
                    removedBlocks.Add(pair.Key);
            }

            return new GridSnapshot
            {
                EntityId = grid.EntityId,
                Name = grid.DisplayName,
                Blocks = changedBlocks,
                RemovedBlocks = removedBlocks
            };
        }

        private void Add(MySlimBlock block)
        {
            lock (@lock)
                blocks[block.Position] = new BlockSnapshot(block);
            block.FatBlock?.Subparts.Values.ForEach(trackedEntities.Add);
        }

        private void Remove(MySlimBlock block)
        {
            lock (@lock)
            {
                if (blocks.ContainsKey(block.Position))
                    blocks.Remove(block.Position);
                else
                    blocks[block.Position] = null;
            }
        }

        public void Dispose()
        {
            grid.OnBlockAdded -= Add;
        }
    }
}