using System;
using System.IO;
using System.Text;
using VRageMath;

namespace NeverSerender.Output
{
    public class BinWriter
    {
        private readonly byte[] buffer = new byte[16];

        public BinWriter(Stream stream)
        {
            BaseStream = stream;
        }

        public Stream BaseStream { get; }

        public void WriteRaw(byte[] bytes)
        {
            BaseStream.Write(bytes, 0, bytes.Length);
        }

        public void WriteRaw(string value)
        {
            WriteRaw(Encoding.UTF8.GetBytes(value));
        }

        public void Write(bool value)
        {
            BaseStream.WriteByte((byte)(value ? 1 : 0));
        }

        public void Write(byte value)
        {
            BaseStream.WriteByte(value);
        }

        public void Write(sbyte value)
        {
            BaseStream.WriteByte((byte)value);
        }

        public void Write(short value)
        {
            Write((ushort)value);
        }

        public void Write(ushort value)
        {
            buffer[0] = (byte)(value >> 8);
            buffer[1] = (byte)value;
            BaseStream.Write(buffer, 0, 2);
        }

        public void Write(int value)
        {
            Write((uint)value);
        }

        public void Write(uint value)
        {
            buffer[0] = (byte)(value >> 24);
            buffer[1] = (byte)(value >> 16);
            buffer[2] = (byte)(value >> 8);
            buffer[3] = (byte)value;
            BaseStream.Write(buffer, 0, 4);
        }

        public void Write(long value)
        {
            Write((ulong)value);
        }

        public void Write(ulong value)
        {
            buffer[0] = (byte)(value >> 56);
            buffer[1] = (byte)(value >> 48);
            buffer[2] = (byte)(value >> 40);
            buffer[3] = (byte)(value >> 32);
            buffer[4] = (byte)(value >> 24);
            buffer[5] = (byte)(value >> 16);
            buffer[6] = (byte)(value >> 8);
            buffer[7] = (byte)value;
            BaseStream.Write(buffer, 0, 8);
        }

        public void Write(float value)
        {
            var bytes = BitConverter.GetBytes(value);
            buffer[0] = bytes[3];
            buffer[1] = bytes[2];
            buffer[2] = bytes[1];
            buffer[3] = bytes[0];
            BaseStream.Write(buffer, 0, 4);
        }

        public void Write(double value)
        {
            var bytes = BitConverter.GetBytes(value);
            buffer[0] = bytes[7];
            buffer[1] = bytes[6];
            buffer[2] = bytes[5];
            buffer[3] = bytes[4];
            buffer[4] = bytes[3];
            buffer[5] = bytes[2];
            buffer[6] = bytes[1];
            buffer[7] = bytes[0];
            BaseStream.Write(buffer, 0, 8);
        }

        public void Write(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            Write((uint)bytes.Length);
            WriteRaw(bytes);
        }

        public void Write(Vector2 vector)
        {
            Write(vector.X);
            Write(vector.Y);
        }

        public void Write(Vector3 vector)
        {
            Write(vector.X);
            Write(vector.Y);
            Write(vector.Z);
        }

        public void Write(Vector3UByte vector)
        {
            Write(vector.X);
            Write(vector.Y);
            Write(vector.Z);
        }
        
        public void Write(Vector3S vector)
        {
            Write(vector.X);
            Write(vector.Y);
            Write(vector.Z);
        }

        public void Write(Vector3I vector)
        {
            Write(vector.X);
            Write(vector.Y);
            Write(vector.Z);
        }

        public void Write(Matrix matrix)
        {
            Write(matrix.M11);
            Write(matrix.M12);
            Write(matrix.M13);
            Write(matrix.M14);

            Write(matrix.M21);
            Write(matrix.M22);
            Write(matrix.M23);
            Write(matrix.M24);

            Write(matrix.M31);
            Write(matrix.M32);
            Write(matrix.M33);
            Write(matrix.M34);

            Write(matrix.M41);
            Write(matrix.M42);
            Write(matrix.M43);
            Write(matrix.M44);
        }

        public void Write(MatrixD matrix)
        {
            Write(matrix.M11);
            Write(matrix.M12);
            Write(matrix.M13);
            Write(matrix.M14);

            Write(matrix.M21);
            Write(matrix.M22);
            Write(matrix.M23);
            Write(matrix.M24);

            Write(matrix.M31);
            Write(matrix.M32);
            Write(matrix.M33);
            Write(matrix.M34);

            Write(matrix.M41);
            Write(matrix.M42);
            Write(matrix.M43);
            Write(matrix.M44);
        }

        public void Write(MatrixI matrix)
        {
            var value = (int)matrix.Forward;
            value += (int)matrix.Up * 6;
            value += (int)matrix.Right * 36;
            Write((byte)value);
        }

        public void PropertyId(long value)
        {
            Write((ushort)Output.PropertyId.Id);
            Write(value);
        }

        public void PropertyName(string value)
        {
            Write((ushort)Output.PropertyId.Name);
            Write(value);
        }

        public void PropertyAuthor(string value)
        {
            Write((ushort)Output.PropertyId.Author);
            Write(value);
        }

        public void PropertyPath(string value)
        {
            Write((ushort)Output.PropertyId.Path);
            Write(value);
        }

        public void PropertyMatrix(Matrix value)
        {
            Write((ushort)Output.PropertyId.Matrix);
            Write(value);
        }

        public void PropertyMatrixD(MatrixD value)
        {
            Write((ushort)Output.PropertyId.MatrixD);
            Write(value);
        }

        public void PropertyTextureType(TextureType value)
        {
            Write((ushort)Output.PropertyId.TextureType);
            Write((byte)value);
        }

        public void PropertyVertices(Action<BinWriter> value)
        {
            Write((ushort)Output.PropertyId.Vertices);
            WriteSized(value);
        }

        public void PropertyNormals(Action<BinWriter> value)
        {
            Write((ushort)Output.PropertyId.Normals);
            WriteSized(value);
        }

        public void PropertyTexCoords(Action<BinWriter> value)
        {
            Write((ushort)Output.PropertyId.TexCoords);
            WriteSized(value);
        }

        public void PropertyIndices(Action<BinWriter> value)
        {
            Write((ushort)Output.PropertyId.Indices);
            WriteSized(value);
        }

        public void PropertyMeshes(Action<BinWriter> value)
        {
            Write((ushort)Output.PropertyId.Meshes);
            WriteSized(value);
        }

        public void PropertyMaterialOverrides(Action<BinWriter> value)
        {
            Write((ushort)Output.PropertyId.MaterialOverrides);
            WriteSized(value);
        }

        public void PropertyModel(uint id)
        {
            Write((ushort)Output.PropertyId.Model);
            Write(id);
        }

        public void PropertyColor(Vector3 value)
        {
            Write((ushort)Output.PropertyId.Color);
            Write(value);
        }
        
        public void PropertyColorMask(Vector3UByte value)
        {
            Write((ushort)Output.PropertyId.ColorMask);
            Write(value);
        }

        public void PropertyDelta(float value)
        {
            Write((ushort)Output.PropertyId.Delta);
            Write(value);
        }

        public void PropertyCone(Vector2 cone)
        {
            Write((ushort)Output.PropertyId.Cone);
            Write(cone);
        }

        public void PropertyScale(float scale)
        {
            Write((ushort)Output.PropertyId.Scale);
            Write(scale);
        }

        public void PropertyRemove()
        {
            Write((ushort)Output.PropertyId.Remove);
        }

        public void PropertyPreview(bool value)
        {
            Write((ushort)Output.PropertyId.Preview);
            Write(value);
        }

        public void PropertyParent(uint value)
        {
            Write((ushort)Output.PropertyId.Parent);
            Write(value);
        }

        public void PropertyShow(bool value)
        {
            Write((ushort)Output.PropertyId.Show);
            Write(value);
        }

        public void PropertyRenderMode(RenderMode value)
        {
            Write((ushort)Output.PropertyId.RenderMode);
            Write((byte)value);
        }

        public void PropertyTexture(TextureKind kind, uint value)
        {
            Write((ushort)Output.PropertyId.Texture);
            Write((byte)kind);
            Write(value);
        }
        
        public void PropertyVector3(Vector3 value)
        {
            Write((ushort)Output.PropertyId.Vector3);
            Write(value);
        }
        
        public void PropertyVector3S(Vector3S value)
        {
            Write((ushort)Output.PropertyId.Vector3S);
            Write(value);
        }
        
        public void PropertyOrientation(MatrixI orientation)
        {
            Write((ushort)Output.PropertyId.Orientation);
            Write(orientation);
        }

        public void PropertyEnd()
        {
            Write((ushort)Output.PropertyId.EndHeader);
        }


        public void Event(EventId eventId, uint id, Action<BinWriter> action = null)
        {
            Write((ushort)0xC080);
            Write((ushort)eventId);
            Write(id);
            WriteSized(action);
        }

        public void WriteSized(Action<BinWriter> action = null)
        {
            if (action == null)
            {
                Write((uint)0);
            }
            else if (BaseStream.CanSeek)
            {
                var sizePos = BaseStream.Position;
                Write(0xFFFF_FFFF); // Placeholder
                action(this);
                var endPos = BaseStream.Position;
                var size = endPos - sizePos - 4;

                BaseStream.Position = sizePos;
                Write((uint)size);

                BaseStream.Position = endPos;
            }
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    action(new BinWriter(memoryStream));
                    var data = memoryStream.ToArray();
                    Write((uint)data.Length);
                    WriteRaw(data);
                }
            }
        }
    }
}