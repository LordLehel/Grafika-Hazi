using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using System;
using System.Collections.Generic;

namespace Grafika_Hazi
{
    internal static class Program
    {
        // The main entry point for the application.
        private static IWindow window = null!;
        private static GL Gl = null!;
        private static uint program;

        private static Camera camera = null!;
        private static RubikCube rubikCube = null!;
        private static RubikController rubikController = null!;
        private static InputHandler inputHandler = null!;

        // Uniform locations
        private static int modelLoc, viewLoc, projLoc, sliceRotLoc;

        // Vertex and Fragment shader source code
        private static readonly string VertexShaderSource = @"
            #version 330 core
            layout (location = 0) in vec3 vPos;
            layout (location = 1) in vec4 vCol;

            out vec4 outCol;

            uniform mat4 uModel;
            uniform mat4 uSliceRotation;
            uniform mat4 uView;
            uniform mat4 uProjection;
    
            void main()
            {
                outCol = vCol;
                gl_Position = uProjection * uView * uSliceRotation * uModel * vec4(vPos, 1.0);
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
            windowOptions.Size = new Vector2D<int>(800, 600);
            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }

        private static unsafe void Window_Load()
        {
            Gl = window.CreateOpenGL();
            Gl.ClearColor(System.Drawing.Color.White);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);

            LinkProgram();

            // Initialize the camera, Rubik's cube, and input handler
            camera = new Camera(new Vector3D<float>(5f, 5f, 6f), Vector3D<float>.Zero);
            rubikCube = new RubikCube(Gl);
            rubikController = new RubikController();

            IInputContext inputContext = window.CreateInput();
            inputHandler = new InputHandler(window, inputContext, camera, rubikController);

            // Shader program setup
            Gl.UseProgram(program);
            modelLoc = Gl.GetUniformLocation(program, "uModel");
            sliceRotLoc = Gl.GetUniformLocation(program, "uSliceRotation");
            viewLoc = Gl.GetUniformLocation(program, "uView");
            projLoc = Gl.GetUniformLocation(program, "uProjection");

            var projMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 4), 800f / 600f, 0.1f, 100f);
            Gl.UniformMatrix4(projLoc, 1, false, (float*)&projMatrix);
        }

        private static void Window_Update(double deltaTime)
        {
            inputHandler.Update(deltaTime);
            rubikController.Update(deltaTime);
        }
        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Gl.UseProgram(program);

            var viewMatrix = camera.GetViewMatrix();
            Gl.UniformMatrix4(viewLoc, 1, false, (float*)&viewMatrix);

            rubikCube.Render(modelLoc, sliceRotLoc, rubikController);
        }

        private static void Window_Closing()
        {
            rubikCube.Dispose();
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
