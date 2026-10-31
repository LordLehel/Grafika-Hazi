using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Grafika_Hazi
{
    // Represents a single piece of the Rubik's cube
    // Each piece has its own VAO, color VBO, model matrix, and grid position
    public class RubikPiece
    {
        public uint Vao { get; set; }
        public uint ColorVbo { get; set; }
        public Matrix4X4<float> ModelMatrix { get; set; }
        public int GridX { get; set; }
        public int GridY { get; set; }
        public int GridZ { get; set; }
    }

    public class RubikCube : IDisposable
    {
        private readonly GL gl;
        private uint sharedVbo;
        private uint sharedEbo;

        public List<RubikPiece> Pieces { get; } = new();

        public RubikCube(GL gl)
        {
            this.gl = gl;
            Initialize();
        }

        private unsafe void Initialize()
        {
            float[] vertexArray = {
                // Front
                -0.5f, -0.5f,  0.5f,   0.5f, -0.5f,  0.5f,   0.5f,  0.5f,  0.5f,  -0.5f,  0.5f,  0.5f,
                // Back
                -0.5f, -0.5f, -0.5f,   0.5f, -0.5f, -0.5f,   0.5f,  0.5f, -0.5f,  -0.5f,  0.5f, -0.5f,
                // Left
                -0.5f, -0.5f, -0.5f,  -0.5f, -0.5f,  0.5f,  -0.5f,  0.5f,  0.5f,  -0.5f,  0.5f, -0.5f,
                // Right
                 0.5f, -0.5f, -0.5f,   0.5f, -0.5f,  0.5f,   0.5f,  0.5f,  0.5f,   0.5f,  0.5f, -0.5f,
                // Top
                -0.5f,  0.5f,  0.5f,   0.5f,  0.5f,  0.5f,   0.5f,  0.5f, -0.5f,  -0.5f,  0.5f, -0.5f,
                // Bottom
                -0.5f, -0.5f,  0.5f,   0.5f, -0.5f,  0.5f,   0.5f, -0.5f, -0.5f,  -0.5f, -0.5f, -0.5f,
            };

            uint[] indexArray = {
                0, 1, 2, 0, 2, 3,       // Front
                5, 4, 7, 5, 7, 6,       // Back
                8, 9, 10, 8, 10, 11,    // Left
                13, 12, 15, 13, 15, 14, // Right
                16, 17, 18, 16, 18, 19, // Top
                21, 20, 23, 21, 23, 22  // Bottom
            };

            // Common VBO and EBO
            sharedVbo = gl.GenBuffer();
            gl.BindBuffer(GLEnum.ArrayBuffer, sharedVbo);
            gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);

            sharedEbo = gl.GenBuffer();
            gl.BindBuffer(GLEnum.ElementArrayBuffer, sharedEbo);
            gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);

            float gap = 1.05f;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        uint vao = gl.GenVertexArray();
                        gl.BindVertexArray(vao);

                        gl.BindBuffer(GLEnum.ElementArrayBuffer, sharedEbo);

                        gl.BindBuffer(GLEnum.ArrayBuffer, sharedVbo);
                        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
                        gl.EnableVertexAttribArray(0);

                        float[] colorArray = GetRubikPieceColors(x, y, z);
                        uint colorVbo = gl.GenBuffer();
                        gl.BindBuffer(GLEnum.ArrayBuffer, colorVbo);
                        gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
                        gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
                        gl.EnableVertexAttribArray(1);

                        var model = Matrix4X4.CreateTranslation(x * gap, y * gap, z * gap);

                        Pieces.Add(new RubikPiece
                        {
                            Vao = vao,
                            ColorVbo = colorVbo,
                            ModelMatrix = model,
                            GridX = x,
                            GridY = y,
                            GridZ = z
                        });
                    }
                }
            }

            gl.BindVertexArray(0);
        }

        private static float[] GetRubikPieceColors(int x, int y, int z)
        {
            var red = new float[] { 1f, 0f, 0f, 1f };
            var orange = new float[] { 1f, 0.5f, 0f, 1f };
            var green = new float[] { 0f, 1f, 0f, 1f };
            var blue = new float[] { 0f, 0f, 1f, 1f };
            var white = new float[] { 1f, 1f, 1f, 1f };
            var yellow = new float[] { 1f, 1f, 0f, 1f };
            var black = new float[] { 0.15f, 0.15f, 0.15f, 1f };

            var colors = new List<float>();
            void AddFaceColor(float[] color)
            {
                for (int i = 0; i < 4; i++) colors.AddRange(color);
            }

            AddFaceColor(z == 1 ? red : black);
            AddFaceColor(z == -1 ? orange : black);
            AddFaceColor(x == -1 ? green : black);
            AddFaceColor(x == 1 ? blue : black);
            AddFaceColor(y == 1 ? white : black);
            AddFaceColor(y == -1 ? yellow : black);

            return colors.ToArray();
        }

        public unsafe void Render(int modelLoc, int sliceRotLoc, RubikController controller)
        {
            foreach (var piece in Pieces)
            {
                gl.BindVertexArray(piece.Vao);

                var model = piece.ModelMatrix;
                gl.UniformMatrix4(modelLoc, 1, false, (float*)&model);

                var sliceRot = controller.GetSliceRotationMatrix(piece);
                gl.UniformMatrix4(sliceRotLoc, 1, false, (float*)&sliceRot);

                gl.DrawElements(GLEnum.Triangles, 36, GLEnum.UnsignedInt, null);
            }

            gl.BindVertexArray(0);
        }

        public void Dispose()
        {
            foreach (var piece in Pieces)
            {
                gl.DeleteBuffer(piece.ColorVbo);
                gl.DeleteVertexArray(piece.Vao);
            }
            gl.DeleteBuffer(sharedVbo);
            gl.DeleteBuffer(sharedEbo);
        }
    }
}