using System;
using System.Collections.Generic;
using System.Linq;
using NeverSerender.Snapshot;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRageMath;

namespace NeverSerender.Changes
{
    public class EntityChangeTracker : IDisposable
    {
        private readonly Dictionary<long, EntitySnapshot> changes = new Dictionary<long, EntitySnapshot>();
        private readonly Dictionary<long, MyEntity> entities = new Dictionary<long, MyEntity>();
        private readonly Dictionary<long, Matrix> localMatrices = new Dictionary<long, Matrix>();
        private readonly MiniLog log;
        private readonly Dictionary<long, MatrixD> worldMatrices = new Dictionary<long, MatrixD>();

        public EntityChangeTracker(MiniLog log)
        {
            this.log = log;
        }

        public void Dispose()
        {
            foreach (var entity in entities.Values)
                Unlink(entity);
        }

        public List<EntitySnapshot> Next()
        {
            var result = changes.Values.ToList();
            changes.Clear();
            return result;
        }

        public void Add(MyEntity entity)
        {
            if (entities.ContainsKey(entity.EntityId))
                return;

            log.WriteLine($"Add DebugName={entity.DebugName} Parent={entity.Parent?.DebugName}");

            entities.Add(entity.EntityId, entity);

            entity.AddedToScene += Show;
            entity.RemovedFromScene += Hide;
            entity.OnClose += Remove;
            entity.PositionComp.OnPositionChanged += Move;

            localMatrices.Add(entity.EntityId, entity.PositionComp.LocalMatrixRef);
            worldMatrices.Add(entity.EntityId, entity.WorldMatrix);

            changes[entity.EntityId] = new EntitySnapshot(entity);

            // Parts are entities too
            entity.Subparts?.Values.ForEach(Add);
        }

        private void Show(MyEntity entity)
        {
            log.WriteLine($"Show DebugName={entity.DebugName}");
            GetChange(entity.EntityId).Show = true;
        }

        private void Hide(MyEntity entity)
        {
            log.WriteLine($"Hide DebugName={entity.DebugName}");
            GetChange(entity.EntityId).Show = false;
        }

        private void Remove(MyEntity entity)
        {
            if (!entities.ContainsKey(entity.EntityId))
                return;

            log.WriteLine($"Remove DebugName={entity.DebugName}");

            Unlink(entity);

            var change = GetChange(entity.EntityId);
            change.Show = false;
            change.Remove = true;

            localMatrices.Remove(entity.EntityId);
            entities.Remove(entity.EntityId);
        }

        private void Move(MyPositionComponentBase position)
        {
            var entity = position.Entity;
            var entityId = entity.EntityId;
            if (entity.Parent != null)
            {
                var oldLocalMatrix = localMatrices[entityId];
                var localMatrix = position.LocalMatrixRef;

                if (oldLocalMatrix == localMatrix) return;

                localMatrices[entityId] = localMatrix;
                GetChange(entityId).LocalMatrix = localMatrix;
            }
            else
            {
                var oldWorldMatrix = worldMatrices[entityId];
                var worldMatrix = position.WorldMatrixRef;

                if (oldWorldMatrix == worldMatrix) return;

                worldMatrices[entityId] = worldMatrix;
                GetChange(entityId).WorldMatrix = worldMatrix;
            }
        }

        private void Unlink(MyEntity entity)
        {
            entity.AddedToScene -= Show;
            entity.RemovedFromScene -= Hide;
            entity.OnClose -= Remove;
            entity.PositionComp.OnPositionChanged -= Move;
        }

        private EntitySnapshot GetChange(long entityId)
        {
            if (changes.TryGetValue(entityId, out var entity))
                return entity;

            entity = new EntitySnapshot
            {
                EntityId = entityId
            };
            changes[entityId] = entity;
            return entity;
        }
    }
}