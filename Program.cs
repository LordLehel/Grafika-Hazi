using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
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

        // az ImGui vezerlo
        private static ImGuiController imGuiController = null!;

        // Uniform locations
        private static int modelLoc, viewLoc, projLoc, sliceRotLoc, isSelLoc;
        // a Phong-arnyalas uniformjai
        private static int lightColorLoc, lightPosLoc, viewPosLoc;

        // a felulettel allithato ertekek: a feny szine (r, g, b) es helye (x, y, z)
        private static System.Numerics.Vector3 lightColor = new(1f, 1f, 1f);
        private static System.Numerics.Vector3 lightPos = new(3f, 5f, 7f);

        // a vertex shader tovabbitja a normalist es a vilagbeli poziciot is
        private static readonly string VertexShaderSource = @"
            #version 330 core
            layout (location = 0) in vec3 vPos;
            layout (location = 1) in vec4 vCol;
            layout (location = 2) in float vIsInner; // 1.0 for inner pieces, 0.0 for outer pieces
            layout (location = 3) in vec3 vNormal;

            out vec4 outCol;
            out vec3 outNormal;
            out vec3 outWorldPos;

            uniform mat4 uModel;
            uniform mat4 uSliceRotation;
            uniform mat4 uView;
            uniform mat4 uProjection;
            uniform int uIsSelected;

            void main()
            {
                outCol = vCol;

                // Visual effect: If the piece is selected and it's an inner piece, change its color to neon green
                if (uIsSelected == 1 && vIsInner > 0.5)
                {
                    outCol = vec4(0.0, 1.0, 0.0, 1.0);
                }

                // A modell- es a szelet-forgatas csak eltolast es forgatast tartalmaz, ezert
                // a normalis a matrix 3x3-as reszevel helyesen elforgathato
                mat4 world = uSliceRotation * uModel;
                vec4 worldPos = world * vec4(vPos, 1.0);

                outWorldPos = worldPos.xyz;
                outNormal = mat3(world) * vNormal;

                gl_Position = uProjection * uView * worldPos;
            }
            ";

        // Phong-arnyalas a fragment shaderben
        private static readonly string FragmentShaderSource = @"
            #version 330 core
            out vec4 FragColor;

            in vec4 outCol;
            in vec3 outNormal;
            in vec3 outWorldPos;

            uniform vec3 uLightColor;
            uniform vec3 uLightPos;
            uniform vec3 uViewPos;

            // az illuminacios egyutthatok szinkomponensenkent (r, g, b)
            const vec3 AmbientStrength = vec3(0.25);
            const vec3 DiffuseStrength = vec3(0.7);
            const vec3 SpecularStrength = vec3(0.4);
            const float Shininess = 32.0;

            void main()
            {
                vec3 norm = normalize(outNormal);
                vec3 lightDir = normalize(uLightPos - outWorldPos);
                vec3 viewDir = normalize(uViewPos - outWorldPos);
                vec3 reflectDir = reflect(-lightDir, norm);

                vec3 ambient = AmbientStrength * uLightColor;

                float diff = max(dot(norm, lightDir), 0.0);
                vec3 diffuse = diff * DiffuseStrength * uLightColor;

                float spec = pow(max(dot(viewDir, reflectDir), 0.0), Shininess);
                vec3 specular = spec * SpecularStrength * uLightColor;

                // az ambiens es a diffuz tag a felulet szinet viszi, a csillanas a fenyet
                vec3 result = (ambient + diffuse) * outCol.rgb + specular;

                FragColor = vec4(result, outCol.a);
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
            rubikController = new RubikController(rubikCube);

            // Scramble the cube
            rubikController.Scramble(30);

            IInputContext inputContext = window.CreateInput();
            inputHandler = new InputHandler(window, inputContext, camera, rubikController);

            // az ImGui vezerlo letrehozasa, ugyanazt a bemeneti kontextust hasznalja
            imGuiController = new ImGuiController(Gl, window, inputContext);

            // Shader program setup
            Gl.UseProgram(program);
            modelLoc = Gl.GetUniformLocation(program, "uModel");
            sliceRotLoc = Gl.GetUniformLocation(program, "uSliceRotation");
            viewLoc = Gl.GetUniformLocation(program, "uView");
            projLoc = Gl.GetUniformLocation(program, "uProjection");

            // most lekerjuk az isSelLoc erteket
            isSelLoc = Gl.GetUniformLocation(program, "uIsSelected");
            lightColorLoc = Gl.GetUniformLocation(program, "uLightColor");
            lightPosLoc = Gl.GetUniformLocation(program, "uLightPos");
            viewPosLoc = Gl.GetUniformLocation(program, "uViewPos");

            var projMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 4), 800f / 600f, 0.1f, 100f);
            Gl.UniformMatrix4(projLoc, 1, false, (float*)&projMatrix);
        }

        private static void Window_Update(double deltaTime)
        {
            inputHandler.Update(deltaTime);
            rubikController.Update(deltaTime);

            // az ImGui bemenetenek frissitese
            imGuiController.Update((float)deltaTime);
        }
        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Gl.UseProgram(program);

            var viewMatrix = camera.GetViewMatrix();
            Gl.UniformMatrix4(viewLoc, 1, false, (float*)&viewMatrix);

            // a Phong-arnyalas uniformjainak atadasa minden kepkockaban
            Gl.Uniform3(lightColorLoc, lightColor.X, lightColor.Y, lightColor.Z);
            Gl.Uniform3(lightPosLoc, lightPos.X, lightPos.Y, lightPos.Z);
            Gl.Uniform3(viewPosLoc, camera.Position.X, camera.Position.Y, camera.Position.Z);

            rubikCube.Render(modelLoc, sliceRotLoc, isSelLoc, rubikController);

            DrawUi();

            imGuiController.Render();
        }

        // a grafikus felulet
        private static void DrawUi()
        {
            ImGuiNET.ImGui.Begin("Vilagitas", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize);
            ImGuiNET.ImGui.SliderFloat3("Feny szine (R, G, B)", ref lightColor, 0f, 1f);
            ImGuiNET.ImGui.InputFloat3("Feny helye (X, Y, Z)", ref lightPos);
            ImGuiNET.ImGui.End();

            // Forgatas
            ImGuiNET.ImGui.Begin("Forgatas", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize);

            // aktualisan kijelolt szelet
            ImGuiNET.ImGui.Text($"Kijelolt: {rubikController.SelectedAxis}, index: {rubikController.SelectedIndex}");

            // sorok (1, 2, 3)
            ImGuiNET.ImGui.Text("Sor:");
            if (ImGuiNET.ImGui.Button("Felso (1)")) rubikController.SelectRow(1);
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Kozep (2)")) rubikController.SelectRow(2);
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Also (3)")) rubikController.SelectRow(3);

            // oszlopok (4, 5, 6) es a X/Z tengely valto
            ImGuiNET.ImGui.Text("Oszlop:");
            if (ImGuiNET.ImGui.Button("Bal (4)")) rubikController.SelectColumn(1);
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Kozep (5)")) rubikController.SelectColumn(2);
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Jobb (6)")) rubikController.SelectColumn(3);

            string depthLabel = rubikController.IsDepthMode ? "Oszlop mod: Z (melyseg)" : "Oszlop mod: X";
            if (ImGuiNET.ImGui.Button(depthLabel + " (Tab)")) rubikController.ToggleDepthMode();

            // forgatas ket iranyba (Q, E) es keveres
            ImGuiNET.ImGui.Text("Forgatas:");
            if (ImGuiNET.ImGui.Button("Negativ (Q)")) rubikController.RotateNegative();
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Pozitiv (E)")) rubikController.RotatePositive();

            if (ImGuiNET.ImGui.Button("Kevers")) rubikController.Scramble(30);

            ImGuiNET.ImGui.End();
        }

        private static void Window_Closing()
        {
            imGuiController.Dispose();
            rubikCube.Dispose();
            Gl.DeleteProgram(program);
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            // forditasi hiba eseten kivetel, hogy ne maradjon eszrevetlen
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader));

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
