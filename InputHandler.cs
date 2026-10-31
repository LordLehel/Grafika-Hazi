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

        public InputHandler(IWindow window, IInputContext inputContext, Camera camera, RubikController rubikController)
        {
            this.window = window;
            this.camera = camera;
            this.rubikController = rubikController;

            keyboard = inputContext.Keyboards[0];
            mouse = inputContext.Mice[0];

            // Set the mouse cursor to raw mode for better camera control
            mouse.Cursor.CursorMode = CursorMode.Raw;
            mouse.MouseMove += OnMouseMove;

            keyboard.KeyDown += OnKeyDown;
        }

        public void Update(double deltaTime)
        {
            float dt = (float)deltaTime;

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
            if (key == Key.Escape)
            {
                window.Close();
            }
            // Rotating the slice: R - 90 degrees clockwise, Backspace - 90 degrees counterclockwise
            else if (key == Key.R)
            {
                rubikController.RotateClockwise();
            }
            else if (key == Key.Backspace)
            {
                rubikController.RotateCounterClockwise();
            }
        }

        private void OnMouseMove(IMouse m, Vector2 position)
        {
            camera.ProcessMouseMovement(position);
        }
    }
}