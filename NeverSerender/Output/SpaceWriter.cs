using System;
using System.Collections.Generic;
using System.IO;
using NeverSerender.Snapshot;
using VRageMath;

namespace NeverSerender.Output
{
    public class SpaceWriter
    {
        private const ushort Version = 2;

        private static readonly byte[] Magic = { 0x6e, 0x53, 0x45, 0x72 }; // "nSEr"

        private readonly MiniLog log;

        private readonly bool useSeek;

        private readonly BinWriter writer;

        public SpaceWriter(Stream stream, MiniLog log)
        {
            writer = new BinWriter(stream);
            useSeek = stream.CanSeek;
            this.log = log;
        }

        public void Header(string name, string author)
        {
            writer.WriteRaw(Magic);
            writer.Write((ushort)0x0000);
            writer.Write(Version);
            writer.Write((uint)0x0000_0000);

            writer.PropertyName(name);
            writer.PropertyAuthor(author);
            writer.PropertyEnd();
        }

        public void End()
        {
            writer.Event(Event.End, w =>
            {
                w.Write((uint)0);
                w.PropertyEnd();
            });
        }

        /// <summary>
        ///     Advances the simulation time by <paramref name="delta" /> seconds
        /// </summary>
        /// <param name="delta"></param>
        public void Advance(float delta)
        {
            writer.Event(Event.Advance, w =>
            {
                // log.WriteLine($"Advance delta={delta}");
                w.Write((uint)0);
                w.PropertyDelta(delta);
                w.PropertyEnd();
            });
        }

        public void Light(uint id, MatrixD matrix, Vector3 color, Vector2? cone)
        {
            writer.Event(Event.Light, w =>
            {
                // log.WriteLine($"Light id={id} color={color} cone={cone}");
                w.Write(id);
                w.PropertyMatrixD(matrix);
                w.PropertyColor(color);
                if (cone.HasValue)
                    w.PropertyCone(cone.Value);
                w.PropertyEnd();
            });
        }

        public void RemoveLight(uint id)
        {
            writer.Event(Event.Light, w =>
            {
                // log.WriteLine($"Light id={id} Remove");
                w.Write(id);
                w.PropertyRemove();
                w.PropertyEnd();
            });
        }

        public void RemoveEntity(uint id)
        {
            writer.Event(Event.Entity, w =>
            {
                // log.WriteLine($"Entity id={id} Remove");
                w.Write(id);
                w.PropertyRemove();
                w.PropertyEnd();
            });
        }

        public void Entity(uint id, EntitySnapshot entity, uint? model)
        {
            writer.Event(Event.Entity, w =>
            {
                log.WriteLine(
                    $"Entity id={id} parent={entity.Parent} name={entity.Name} model={entity.Model} color={entity.Color} preview={entity.IsPreview}");
                w.Write(id);
                w.PropertyId(entity.EntityId);
                if (entity.Parent.HasValue)
                    w.PropertyParent(entity.Parent.Value);
                if (entity.Name != null)
                    w.PropertyName(entity.Name);
                if (entity.LocalMatrix.HasValue)
                    w.PropertyMatrix(entity.LocalMatrix.Value);
                if (entity.WorldMatrix.HasValue)
                    w.PropertyMatrixD(entity.WorldMatrix.Value);
                if (entity.Color.HasValue)
                    w.PropertyColor(entity.Color.Value);
                if (entity.IsPreview.HasValue)
                    w.PropertyPreview(entity.IsPreview.Value);
                if (entity.Show.HasValue)
                    w.PropertyShow(entity.Show.Value);
                if (entity.Remove)
                    w.PropertyRemove();

                if (model.HasValue)
                    w.PropertyModel(model.Value);
                w.PropertyEnd();
            });
        }

        public void Grid(uint id, float scale, IList<BlockProperties> blocks)
        {
            writer.Event(Event.Grid, w =>
            {
                // log.WriteLine($"Grid id={id} scale={scale} blocks={blocks.Count}");
                w.Write(id);
                w.PropertyScale(scale);
                w.PropertyEnd();
                foreach (var block in blocks)
                {
                    w.Write(block.Position);
                    w.Write(block.Translation);
                    w.Write(block.Orientation);
                    w.Write(block.Color);
                    if (block.EntityId.HasValue)
                        w.PropertyId(block.EntityId.Value);
                    if (block.Name != null)
                        w.PropertyName(block.Name);
                    if (block.Model.HasValue)
                        w.PropertyModel(block.Model.Value);
                    if (block.Remove)
                        w.PropertyRemove();
                    if (block.Modifiers != null)
                        w.PropertyMaterialOverrides(w2 => block.Modifiers.ForEach(m =>
                        {
                            w.Write(m.Item1);
                            w.Write(m.Item2);
                        }));

                    w.PropertyEnd();
                }
            });
        }

        public void Model(uint id, string name,
            Action<BinWriter> writeVertices,
            Action<BinWriter> writeNormals,
            Action<BinWriter> writeTexCoords,
            Action<BinWriter> writeIndices,
            Action<BinWriter> writeMeshes
        )
        {
            writer.Event(Event.Model, w =>
            {
                w.Write(id);
                w.PropertyName(name);
                w.PropertyVertices(writeVertices);
                w.PropertyNormals(writeNormals);
                w.PropertyTexCoords(writeTexCoords);
                w.PropertyIndices(writeIndices);
                w.PropertyMeshes(writeMeshes);
                w.PropertyEnd();
            });
        }

        public void Material(uint id, MaterialProperties props)
        {
            writer.Event(Event.Material,
                w =>
                {
                    w.Write(id);
                    w.PropertyEnd();
                    w.Write(props.ColorMetal);
                    w.Write(props.AddMaps);
                    w.Write(props.NormalGloss);
                    w.Write(props.AlphaMask);
                });
        }

        public void Texture(uint id, TextureType type, string name, string path,
            Action<Stream> writeTexture)
        {
            writer.Event(Event.Texture, w =>
            {
                w.Write(id);
                w.PropertyTextureType(type);
                w.PropertyName(name);
                if (path != null)
                    w.PropertyPath(path);
                w.PropertyEnd();

                writeTexture?.Invoke(w.BaseStream);
            });
        }
    }
}