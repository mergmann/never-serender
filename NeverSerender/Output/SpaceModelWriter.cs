using System;
using System.IO;
using NeverSerender.Config;
using VRageMath;

namespace NeverSerender.Output
{
    public class SpaceModelWriter
    {
        private const ushort MajorVersion = 1;
        private const ushort MinorVersion = 3;

        private static readonly byte[] Magic = { 0x6e, 0x53, 0x45, 0x72 }; // "nSEr"

        private readonly MiniLog log;

        private readonly BinWriter writer;

        public SpaceModelWriter(Stream stream, MiniLog log)
        {
            writer = new BinWriter(stream);
            this.log = log;
        }

        public void Header(string name, string author, MatrixD viewMatrix)
        {
            writer.WriteRaw(Magic);
            writer.Write(MajorVersion);
            writer.Write(MinorVersion);
            writer.Write((uint)0x0000_0000);

            writer.PropertyName(name);
            writer.PropertyAuthor(author);
            writer.PropertyMatrixD(viewMatrix);
            writer.PropertyEnd();
        }

        public void End()
        {
            writer.Event(EventId.End, 0, w => w.PropertyEnd());
        }

        /// <summary>
        ///     Advances the simulation time by <paramref name="delta" /> seconds
        /// </summary>
        /// <param name="delta"></param>
        public void Advance(float delta)
        {
            writer.Event(EventId.Advance, 0, w =>
            {
                // log.WriteLine($"Advance delta={delta}");
                w.PropertyDelta(delta);
                w.PropertyEnd();
            });
        }

        public void Light(uint id, MatrixD matrix, Vector3 color, Vector2? cone)
        {
            writer.Event(EventId.Light, id, w =>
            {
                // log.WriteLine($"Light id={id} color={color} cone={cone}");
                w.PropertyMatrixD(matrix);
                w.PropertyColor(color);
                if (cone.HasValue)
                    w.PropertyCone(cone.Value);
                w.PropertyEnd();
            });
        }

        public void RemoveLight(uint id)
        {
            writer.Event(EventId.Light, id, w =>
            {
                // log.WriteLine($"Light id={id} Remove");
                w.PropertyRemove();
                w.PropertyEnd();
            });
        }

        public void Entity(uint id, EntityProperties entity, uint? model)
        {
            writer.Event(EventId.Entity, id, w =>
            {
                log.WriteLine(
                    $"Entity id={id} parent={entity.Parent} name={entity.Name} model={entity.Model} color={entity.Color} preview={entity.IsPreview}");
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
                    w.PropertyColorMask(entity.Color.Value);
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
        
        public void RemoveEntity(uint id)
        {
            writer.Event(EventId.Entity, id, w =>
            {
                // log.WriteLine($"Entity id={id} Remove");
                w.PropertyRemove();
                w.PropertyEnd();
            });
        }

        public void Block(uint id, BlockProperties block)
        {
            writer.Event(EventId.Block, id, w =>
            {
                w.PropertyParent(block.GridId);
                w.PropertyVector3S(block.Position);
                w.PropertyVector3(block.Translation);
                w.PropertyOrientation(block.Orientation);
                w.PropertyColorMask(block.Color);
                if (block.EntityId != null)
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
            });
        }
        
        public void RemoveBlock(uint id, Vector3S position)
        {
            writer.Event(EventId.Block, id, w =>
            {
                // log.WriteLine($"Block id={id} Remove");
                w.PropertyVector3S(position);
                w.PropertyRemove();
                w.PropertyEnd();
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
            writer.Event(EventId.Model, id, w =>
            {
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
            writer.Event(EventId.Material, id,
                w =>
                {
                    if (props.Name != null)
                        w.PropertyName(props.Name);
                    w.PropertyRenderMode(props.RenderMode);
                    foreach (var texture in props.Textures)
                        w.PropertyTexture(texture.Key, texture.Value);
                    w.PropertyEnd();
                });
        }

        public void Texture(uint id, TextureType type, string name, string path,
            Action<Stream> writeTexture)
        {
            writer.Event(EventId.Texture, id, w =>
            {
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