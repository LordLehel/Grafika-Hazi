using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System.Dynamic;
using System.Numerics;
using System.Reflection;
using Szeminarium;

namespace GrafikaSzeminarium
{
    internal class Program
    {
        private static IWindow graphicWindow;
        private static GL Gl;
        private static ImGuiController imGuiController;

        // lapos normalisokkal rajzolt lap
        private static ModelObjectDescriptor plankFlat;
        // simitott (10 fokkal elfordulo) normalisokkal rajzolt lap
        private static ModelObjectDescriptor plankSmooth;

        private static CameraDescriptor camera = new CameraDescriptor();

        // - A dezsa parameterei -
        // a lapok szama a koron
        private const int PlankCount = 18;
        // egy lap szelessege
        private const float PlankWidth = 1f;

        // szog ket szomszedos lap kozott (20 fok)
        private static readonly float StepAngle = 2f * MathF.PI / PlankCount;
        // a lepes fele (10 fok)
        private static readonly float HalfAngle = StepAngle / 2f;

        private static readonly float Apothem = (PlankWidth / 2f) / MathF.Tan(HalfAngle);
        // a dezsa kulso sugara (a lapok szelso elei)
        private static readonly float OuterRadius = MathF.Sqrt(Apothem * Apothem + (PlankWidth / 2f) * (PlankWidth / 2f));

        // Shader: uniform valtozok
        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";

        private const string ShinenessVariableName = "uShininess";

        // A csillanas erossege
        private static float shininess = 50;

        private static uint program;

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Grafika szeminarium";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(500, 500);

            graphicWindow = Window.Create(windowOptions);

            // Esemenykezelok regisztralasa
            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;
            graphicWindow.Closing += GraphicWindow_Closing;

            graphicWindow.Run();
        }

        // Eroforrasfelszabaditas
        private static void GraphicWindow_Closing()
        {
            plankFlat.Dispose();
            plankSmooth.Dispose();
            Gl.DeleteProgram(program);
        }

        // Inicializalas
        private static void GraphicWindow_Load()
        {
            Gl = graphicWindow.CreateOpenGL();

            // Billentyuzet esemenyek
            var inputContext = graphicWindow.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            // Atmeretezeskor a nezet igazitasa
            graphicWindow.FramebufferResize += s =>
            {
                Gl.Viewport(s);
            };

            imGuiController = new ImGuiController(Gl, graphicWindow, inputContext);

            // A ket lap letrehozasa
            plankFlat = ModelObjectDescriptor.CreatePlank(Gl, false, HalfAngle);
            plankSmooth = ModelObjectDescriptor.CreatePlank(Gl, true, HalfAngle);

            Gl.ClearColor(System.Drawing.Color.White);

            // A hatso lapokat nem rajzoljuk
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(TriangleFace.Back);

            // Melysegteszt
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);

            // Shaderek forditasa
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, GetEmbeddedResourceAsString("Shaders.VertexShader.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, GetEmbeddedResourceAsString("Shaders.FragmentShader.frag"));
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader));

            // Program osszeallitasa es linkelese
            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);

            // Shader objectumok felszabaditasa
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
            if ((ErrorCode)Gl.GetError() != ErrorCode.NoError)
            {

            }

            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
        }

        // A shader forrasat az assembly beagyazott eroforrasai kozul olvassa be
        private static string GetEmbeddedResourceAsString(string resourceRelativePath)
        {
            string resourceFullPath = Assembly.GetExecutingAssembly().GetName().Name + "." + resourceRelativePath;

            using (var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceFullPath))
            using (var resStreamReader = new StreamReader(resStream))
            {
                var text = resStreamReader.ReadToEnd();
                return text;
            }
        }

        // Kamera iranyitas billentyukkel
        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left:
                    camera.DecreaseZYAngle();
                    break;
                case Key.Right:
                    camera.IncreaseZYAngle();
                    break;
                case Key.Down:
                    camera.IncreaseDistance();
                    break;
                case Key.Up:
                    camera.DecreaseDistance();
                    break;
                case Key.U:
                    camera.IncreaseZXAngle();
                    break;
                case Key.D:
                    camera.DecreaseZXAngle();
                    break;
            }
        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            // Itt nem hasznalunk OpenGL-t, csak az ImGui bemenetet frissitjuk
            imGuiController.Update((float)deltaTime);
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetUniform3(LightColorVariableName, new Vector3(1f, 1f, 1f));
            // A feny a kamera helyen van
            SetUniform3(LightPositionVariableName, new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z));
            SetUniform3(ViewPositionVariableName, new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z));
            SetUniform1(ShinenessVariableName, shininess);

            var viewMatrix = Matrix4X4.CreateLookAt(camera.Position, camera.Target, camera.UpVector);
            SetMatrix(viewMatrix, ViewMatrixVariableName);

            // Az oldalarany az ablak akt. meretebol szamolodik
            float aspect = (float)graphicWindow.Size.X / graphicWindow.Size.Y;
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 2), aspect, 0.1f, 100f);
            SetMatrix(projectionMatrix, ProjectionMatrixVariableName);

            // A ket dezsa kozeppontja
            float barrelOffset = OuterRadius + 0.5f;
            DrawBarrel(plankFlat, -barrelOffset);    // bal: a lapok sikjara meroleges normalisok
            DrawBarrel(plankSmooth, barrelOffset);   // jobb: 10 fokkal elfordulo, simitott normalisok

            // ImGui ablak
            ImGuiNET.ImGui.Begin("Lighting", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize | ImGuiNET.ImGuiWindowFlags.NoCollapse);
            ImGuiNET.ImGui.SliderFloat("Shininess", ref shininess, 5, 100);
            ImGuiNET.ImGui.End();

            imGuiController.Render();
        }

        // Hordo kirajzolasa
        private static unsafe void DrawBarrel(ModelObjectDescriptor plank, float xOffset)
        {
            // a teljes dezsa eltolasa oldalra
            var barrelShift = Matrix4X4.CreateTranslation(xOffset, 0f, 0f);

            for (int i = 0; i < PlankCount; i++)
            {
                var model = Matrix4X4.CreateTranslation(0f, 0f, Apothem)
                          * Matrix4X4.CreateRotationY(i * StepAngle)
                          * barrelShift;

                SetModelMatrix(model);
                DrawModelObject(plank);
            }
        }

        // Modell matrix es normal matrix beallitasa
        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            SetMatrix(modelMatrix, ModelMatrixVariableName);

            // normal matrix beallitasa
            int location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }

            // G = (M^-1)^T, az eltolas nelkul
            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));

            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        // float uniform beallitasa
        private static unsafe void SetUniform1(string uniformName, float uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform1(location, uniformValue);
            CheckError();
        }

        // vec3 uniform beallitasa
        private static unsafe void SetUniform3(string uniformName, Vector3 uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform3(location, uniformValue);
            CheckError();
        }

        // modell kirajzolasa
        private static unsafe void DrawModelObject(ModelObjectDescriptor modelObject)
        {
            Gl.BindVertexArray(modelObject.Vao);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, modelObject.Indices);
            Gl.DrawElements(PrimitiveType.Triangles, modelObject.IndexArrayLength, DrawElementsType.UnsignedInt, null);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(0);
        }

        // mat4 uniform beallitasa
        private static unsafe void SetMatrix(Matrix4X4<float> mx, string uniformName)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&mx);
            CheckError();
        }

        // Hibakezeles
        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}
