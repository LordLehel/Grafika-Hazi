using Silk.NET.Input;
using Silk.NET.Windowing;
using System.Numerics;

namespace Grafika_Hazi
{
    // This class handles user input for camera movement and Rubik's cube rotation.
    public class InputHandler
    {
        private readonly IWindow window;
        private readonly Camera camera;
        private readonly RubikController rubikController;
        private readonly IKeyboard keyboard;
        private readonly IMouse mouse;

        // true = felulet-mod
        // false = kamera-mod
        private bool uiMode = true;

        public InputHandler(IWindow window, IInputContext inputContext, Camera camera, RubikController rubikController)
        {
            this.window = window;
            this.camera = camera;
            this.rubikController = rubikController;

            keyboard = inputContext.Keyboards[0];
            mouse = inputContext.Mice[0];

            // indulaskor felulet-mod, a C billentyuvel lehet kamera-modra valtani
            mouse.Cursor.CursorMode = CursorMode.Normal;
            mouse.MouseMove += OnMouseMove;

            keyboard.KeyDown += OnKeyDown;
        }

        public void Update(double deltaTime)
        {
            float dt = (float)deltaTime;

            // ha a felhasznalo gepel, a billentyuk ne mozgassak a kamerat
            if (ImGuiNET.ImGui.GetIO().WantTextInput) return;

            // Camera movement based on keyboard input (In direction of the camera - mouse movement)
            if (keyboard.IsKeyPressed(Key.W)) camera.MoveForward(dt);
            if (keyboard.IsKeyPressed(Key.S)) camera.MoveBackward(dt);
            if (keyboard.IsKeyPressed(Key.D)) camera.MoveRight(dt);
            if (keyboard.IsKeyPressed(Key.A)) camera.MoveLeft(dt);

            // Space - Move Up, Shift - Move Down
            if (keyboard.IsKeyPressed(Key.Space))
            {
                camera.MoveUp(dt);
            }

            if (keyboard.IsKeyPressed(Key.ShiftLeft))
            {
                camera.MoveDown(dt);
            }
        }

        private void OnKeyDown(IKeyboard kb, Key key, int keyCode)
        {
            // szovegmezo szerkesztese kozben a szamok es betuk ne valasszanak szeletet / ne forgassanak
            if (ImGuiNET.ImGui.GetIO().WantTextInput) return;

            switch (key)
            {
                case Key.Escape:
                    window.Close();
                    break;

                // C - valtas felulet-mod es kamera-mod kozott
                case Key.C:
                    uiMode = !uiMode;
                    mouse.Cursor.CursorMode = uiMode ? CursorMode.Normal : CursorMode.Raw;
                    camera.ResetMouse();
                    break;

                // Toggle depth mode
                case Key.Tab:
                    rubikController.ToggleDepthMode();
                    break;

                // Select rows (1, 2, 3)
                case Key.Number1:
                    rubikController.SelectRow(1);
                    break;
                case Key.Number2:
                    rubikController.SelectRow(2);
                    break;
                case Key.Number3:
                    rubikController.SelectRow(3);
                    break;

                // Select columns (4, 5, 6)
                case Key.Number4:
                    rubikController.SelectColumn(1);
                    break;
                case Key.Number5:
                    rubikController.SelectColumn(2);
                    break;
                case Key.Number6:
                    rubikController.SelectColumn(3);
                    break;

                // Rotate the selected slice of the cube
                // Q button: Row -> left (-Y), Column -> down (+X)
                // E button: Row -> right (+Y), Column -> up (-X)
                case Key.Q:
                    rubikController.RotateNegative();
                    break;
                case Key.E:
                    rubikController.RotatePositive();
                    break;
            }
        }

        private void OnMouseMove(IMouse m, Vector2 position)
        {
            // felulet-modban az eger a gombokat kezeli, a kamera nem fordul
            if (uiMode) return;

            camera.ProcessMouseMovement(position);
        }
    }
}