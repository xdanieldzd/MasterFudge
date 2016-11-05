using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reflection;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace MasterFudge
{
    public static class Utils
    {
        static readonly uint[] crcTable;
        static readonly uint crcPolynomial = 0xEDB88320;
        static readonly uint crcSeed = 0xFFFFFFFF;

        static Utils()
        {
            crcTable = new uint[256];

            for (int i = 0; i < 256; i++)
            {
                uint entry = (uint)i;
                for (int j = 0; j < 8; j++)
                {
                    if ((entry & 0x00000001) == 0x00000001)
                        entry = (entry >> 1) ^ crcPolynomial;
                    else
                        entry = (entry >> 1);
                }
                crcTable[i] = entry;
            }
        }

        public static uint CalculateCrc32(byte[] data)
        {
            return CalculateCrc32(data, 0, data.Length);
        }

        public static uint CalculateCrc32(byte[] data, int start, int length)
        {
            uint crc = crcSeed;
            for (int i = start; i < (start + length); i++)
                crc = ((crc >> 8) ^ crcTable[data[i] ^ (crc & 0x000000FF)]);
            return ~crc;
        }

        // TODO: more bit operations and make the rest of the code use them

        public static bool IsBitSet(byte value, int bit)
        {
            return ((value & (1 << bit)) != 0);
        }

        public static class OpenGL
        {
            /* Also adapted from Cobalt */

            public static int GenerateVertexBuffer<TVertex>(TVertex[] vertices) where TVertex : struct, IVertexStruct
            {
                int vboId = GL.GenBuffer();

                Type vertexType = typeof(TVertex);

                List<VertexElement> vertexElements = new List<VertexElement>();
                int vertexStructSize = Marshal.SizeOf(vertexType);

                foreach (FieldInfo field in vertexType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var attribs = field.GetCustomAttributes(typeof(VertexElementAttribute), false);
                    if (attribs == null || attribs.Length != 1) continue;

                    VertexElementAttribute elementAttribute = (attribs[0] as VertexElementAttribute);

                    int numComponents = Marshal.SizeOf(field.FieldType);

                    if (field.FieldType.IsValueType && !field.FieldType.IsEnum)
                    {
                        var structFields = field.FieldType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (structFields == null || structFields.Length < 1 || structFields.Length > 4) throw new Exception("Invalid number of fields in struct");
                        numComponents = structFields.Length;
                    }

                    vertexElements.Add(new VertexElement()
                    {
                        AttributeIndex = elementAttribute.AttributeIndex,
                        DataType = field.FieldType,
                        NumComponents = numComponents,
                        OffsetInVertex = Marshal.OffsetOf(vertexType, field.Name).ToInt32()
                    });
                }

                GL.BindBuffer(BufferTarget.ArrayBuffer, vboId);
                GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(vertexStructSize * vertices.Length), vertices, BufferUsageHint.StaticDraw);

                foreach (VertexElement element in vertexElements)
                {
                    GL.EnableVertexAttribArray(element.AttributeIndex);
                    GL.VertexAttribPointer(element.AttributeIndex, element.NumComponents, GetVertexAttribPointerType(element.DataType), false, vertexStructSize, element.OffsetInVertex);
                }

                return vboId;
            }

            private static VertexAttribPointerType GetVertexAttribPointerType(Type type)
            {
                if (pointerTypeTranslator.ContainsKey(type))
                    return pointerTypeTranslator[type];
                else
                    throw new ArgumentException("Unimplemented or unsupported datatype");
            }

            static readonly Dictionary<Type, VertexAttribPointerType> pointerTypeTranslator = new Dictionary<Type, VertexAttribPointerType>()
            {
                { typeof(byte), VertexAttribPointerType.UnsignedByte },
                { typeof(sbyte), VertexAttribPointerType.Byte },
                { typeof(ushort), VertexAttribPointerType.UnsignedShort },
                { typeof(short), VertexAttribPointerType.Short },
                { typeof(uint), VertexAttribPointerType.UnsignedInt },
                { typeof(int), VertexAttribPointerType.Int },
                { typeof(float), VertexAttribPointerType.Float },
                { typeof(double), VertexAttribPointerType.Double },
                { typeof(Vector2), VertexAttribPointerType.Float },
                { typeof(Vector3), VertexAttribPointerType.Float },
                { typeof(Vector4), VertexAttribPointerType.Float },
                { typeof(Color4), VertexAttribPointerType.Float },
            };

            public interface IVertexStruct { }

            internal class VertexElement
            {
                public int AttributeIndex { get; set; }
                public Type DataType { get; set; }
                public int NumComponents { get; set; }
                public int OffsetInVertex { get; set; }

                public VertexElement()
                {
                    AttributeIndex = -1;
                    DataType = null;
                    NumComponents = -1;
                    OffsetInVertex = -1;
                }
            }

            [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
            public class VertexElementAttribute : Attribute
            {
                public int AttributeIndex { get; set; }

                public VertexElementAttribute()
                {
                    AttributeIndex = -1;
                }
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct Vertex2D : IVertexStruct
            {
                [VertexElement(AttributeIndex = 0)]
                public Vector2 Position;
                [VertexElement(AttributeIndex = 1)]
                public Vector2 TexCoord;
            }
        }
    }
}
