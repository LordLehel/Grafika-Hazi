using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Szeminarium1
{
    internal static class Program
    {
        private static IWindow graphicWindow;

        private static GL Gl;

        private static uint program;

        // Vertex Shader
        // INPUT: vPos - pozíció , vCol - szín
        // OUTPUT: outCol - szín => Fragment Shader, gl_Position - pozíció => Rasterizer
        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";

        // Fragment Shader
        // INPUT: outCol - szín
        // OUTPUT: FragColor - pixel színe => Framebuffer
        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
		
		in vec4 outCol;

        void main()
        {
            FragColor = outCol;
        }
        ";

        static void Main(string[] args)
        {
            // Ablak beállítások konfigurálása
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "1. szeminárium - háromszög";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(500, 500);

            // Ablak példányosítása
            graphicWindow = Window.Create(windowOptions);

            // Eseménykezelők regisztrálása
            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;

            // Fő eseményhurok elindítása
            graphicWindow.Run();
        }

        // Globális hibakezelő metódus
        private static void CheckGLError(string stepName)
        {
            GLEnum error;
            // Az OpenGL sorban lévő hibák ellenőrzése
            while ((error = Gl.GetError()) != GLEnum.NoError)
            {
                Console.WriteLine($"OpenGL error after {stepName}: {error}");
            }
        }

        private static void GraphicWindow_Load()
        {
            Console.WriteLine("> Loading the graphic window...");
            // OpenGL kontextus lekérése a Silk.NET ablakból
            Gl = graphicWindow.CreateOpenGL();
            
            // Háttérszín beállítása (fehér)
            Gl.ClearColor(System.Drawing.Color.White);
            CheckGLError("Setting clear color");

            // Shaderek létrehozása
            Console.WriteLine("> Creating shaders...");
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            // VERTEX SHADER
            // Fordítás
            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);

            // Hibakezelés
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            CheckGLError("Compiling vertex shader");

            // FRAGMENT SHADER
            // Fordítás
            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            // Shaderek összekapcsolása egy programba
            Console.WriteLine("> Linking shaders into a program...");
            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);

            // Takarítás
            Console.WriteLine("> Cleaning up shaders...");
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);

            // Hibakezelés
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }

            CheckGLError("Linking shader program");
        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            // Itt frissül a játék/alkalmazás állapota a képkockák között eltelt idő (deltaTime) alapján.
            // Ide tilos OpenGL (Gl.) hívásokat tenni, ez kizárólag matematikai/fizikai számítások helye.
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            // Itt történik a tényleges rajzolás a képernyőre. A deltaTime paraméter az előző képkocka renderelése óta eltelt időt jelzi.

            // Töröljük a képernyőt a háttérszínnel
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            CheckGLError("Clearing the screen");

            // Vertex Array Object (VAO) létrehozása és bindolása
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            // Vertex adatok (pozíciók és színek) definiálása
            float[] vertexArray = new float[] {
                -0.5f, -0.5f, 0.0f,
                -0.5f, +0.5f, 0.0f,
                +0.5f, +0.5f, 0.0f,
                +0.5f, -0.5f, 0.0f,

                 0.0f, +0.5f, 0.0f,
                +0.5f, 0.0f, 0.0f,
                 0.0f, -0.5f, 0.0f,
                -0.5f, 0.0f, 0.0f
            };

            // Színadatok definiálása (RGBA formátumban)
            float[] colorArray = new float[] {
                1.0f, 0.5f, 0.0f, 1.0f,
                1.0f, 0.99f, 0.5f, 1.0f,
                0.5f, 0.5f, 1.0f, 1.0f,
                0.7f, 0.0f, 1.0f, 1.0f,

                1.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.05f, 0.6f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f
            };

            // Indexek definiálása a háromszög rajzolásához
            uint[] indexArray = new uint[] { 
                0, 1, 2,
                2, 3, 0,
                4, 5, 6,
                6, 7, 4
            };

            // ADATOK GPU-ra másolása

            // Vertex Buffer Object (VBO) létrehozása
            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(0);

            CheckGLError("Setting up vertex buffer");

            // Color Buffer Object (CBO) létrehozása
            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            CheckGLError("Setting up color buffer");

            // EBO (Element Buffer Object) létrehozása
            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);

            CheckGLError("Setting up index buffer");

            // Feloldjuk a buffer bindolását, hogy ne maradjon véletlenül bindolva a következő rendereléshez
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            // Kiválasztjuk a shader programot, amit használni fogunk a rendereléshez
            Gl.UseProgram(program);

            // Renderelés + adatok feloldása
            Gl.DrawElements(GLEnum.Triangles, (uint)indexArray.Length, GLEnum.UnsignedInt, null); // we used element buffer
            
            CheckGLError("Drawing elements");

            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(vao);

            // Takarítás
            Gl.DeleteBuffer(vertices);
            Gl.DeleteBuffer(colors);
            Gl.DeleteBuffer(indices);
            Gl.DeleteVertexArray(vao);

            CheckGLError("Cleaning up buffers and VAO");
        }
    }
}
