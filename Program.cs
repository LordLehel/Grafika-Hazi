using Grafika_Hazi;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;

namespace Grafika_Hazi
{
    internal static class Program
    {
        // The main entry point for the application.
        private static IWindow window = null!;
        private static GL Gl = null!;
        private static uint program;

        // Uniform locations
        private static int modelLoc, viewLoc, projLoc;


        // Rubik's cube pieces
        private struct RubikPiece
        {
            public uint Vao;
            public Matrix4X4<float> ModelMatrix;
        }
        private static List<RubikPiece> rubikPieces = new();


        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";


        // Vertex and Fragment shader source code
        private static readonly string VertexShaderSource = @"
            #version 330 core
            layout (location = 0) in vec3 vPos;
            layout (location = 1) in vec4 vCol;

            out vec4 outCol;

            uniform mat4 uModel;
            uniform mat4 uView;
            uniform mat4 uProjection;
    
            void main()
            {
                outCol = vCol;
                gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
            }
            ";
        private static readonly string FragmentShaderSource = @"
            #version 330 core
            out vec4 FragColor;

		    in vec4 outCol;

            void main()
            {
                FragColor = outCol;
            }
            ";


        // The main entry point for the application.
        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Rubik kocka";
            windowOptions.Size = new Vector2D<int>(500, 500);

            // on some systems there is no depth buffer by default, so we need to make sure one is created
            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }

        private static unsafe void Window_Load()
        {
            Gl = window.CreateOpenGL();
            Gl.ClearColor(System.Drawing.Color.White);

            LinkProgram();

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);


            // Common VBO and EBO for all cubes
            float[] vertexArray = new float[] {
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

            uint[] indexArray = new uint[] {
                    0, 1, 2, 0, 2, 3,       // Front
                    5, 4, 7, 5, 7, 6,       // Back
                    8, 9, 10, 8, 10, 11,    // Left
                    13, 12, 15, 13, 15, 14, // Right
                    16, 17, 18, 16, 18, 19, // Top
                    21, 20, 23, 21, 23, 22  // Bottom
                };

            // Create shared VBO and EBO
            uint sharedVbo = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, sharedVbo);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);

            uint sharedEbo = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, sharedEbo);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);


            // --- Create VAOs for each Rubik's cube piece ---
            float gap = 1.05f; // Small gap between the faces

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        uint vao = Gl.GenVertexArray();
                        Gl.BindVertexArray(vao);

                        // Common index buffer for all cubes
                        Gl.BindBuffer(GLEnum.ElementArrayBuffer, sharedEbo);

                        // Common vertex buffer for the 0th position attribute
                        Gl.BindBuffer(GLEnum.ArrayBuffer, sharedVbo);
                        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
                        Gl.EnableVertexAttribArray(0);

                        // Unique color buffer for each cube piece based on its position
                        float[] colorArray = GetRubikPieceColors(x, y, z);
                        uint colorVbo = Gl.GenBuffer();
                        Gl.BindBuffer(GLEnum.ArrayBuffer, colorVbo);
                        Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
                        Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
                        Gl.EnableVertexAttribArray(1);

                        // Set the model matrix for this piece
                        var model = Matrix4X4.CreateTranslation(x * gap, y * gap, z * gap);

                        rubikPieces.Add(new RubikPiece { Vao = vao, ModelMatrix = model });
                    }
                }
            }

            Gl.BindVertexArray(0);

            // Fixed camera position and projection for the Rubik's cube
            Gl.UseProgram(program);
            modelLoc = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            viewLoc = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            projLoc = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            // Set up the view and projection matrices (the angle of view)
            var viewMatrix = Matrix4X4.CreateLookAt(new Vector3D<float>(5f, 5f, 6f), Vector3D<float>.Zero, Vector3D<float>.UnitY);
            var projMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 4), 600f / 600f, 0.1f, 100f);

            Gl.UniformMatrix4(viewLoc, 1, false, (float*)&viewMatrix);
            Gl.UniformMatrix4(projLoc, 1, false, (float*)&projMatrix);
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
                for (int i = 0; i < 4; i++) colors.AddRange(color); // 1 face = 4 vertices
            }

            // If the piece is on the outer layer, assign the corresponding color; otherwise, assign black
            AddFaceColor(z == 1 ? red : black);       // Front
            AddFaceColor(z == -1 ? orange : black);   // Back
            AddFaceColor(x == -1 ? green : black);    // Left
            AddFaceColor(x == 1 ? blue : black);      // Right
            AddFaceColor(y == 1 ? white : black);     // Top
            AddFaceColor(y == -1 ? yellow : black);   // Bottom

            return colors.ToArray();
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            // We go through each piece of the Rubik's cube and draw it with its own model matrix
            foreach (var piece in rubikPieces)
            {
                Gl.BindVertexArray(piece.Vao);

                var model = piece.ModelMatrix;
                Gl.UniformMatrix4(modelLoc, 1, false, (float*)&model);

                // 36 indices = 6 faces * 2 triangles per face * 3 vertices per triangle
                Gl.DrawElements(GLEnum.Triangles, 36, GLEnum.UnsignedInt, null);
            }

            Gl.BindVertexArray(0);
        }

        private static void Window_Closing()
        {
            // Clean up resources
            Gl.DeleteProgram(program);
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);

            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }
    }
}
