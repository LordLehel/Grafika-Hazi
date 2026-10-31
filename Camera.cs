using Silk.NET.Maths;
using System;
using System.Numerics;

namespace Grafika_Hazi
{
	public class Camera
	{
        public Vector3D<float> Position { get; private set; }
        public Vector3D<float> Front { get; private set; }
        public Vector3D<float> Up { get; private set; }
        public Vector3D<float> Right { get; private set; }

        private float yaw;
        private float pitch;

        private const float MoveSpeed = 4.5f;
        private const float MouseSensitivity = 0.12f;

        private bool firstMouseMove = true;
        private Vector2 lastMousePos;


        // Constructor that initializes the camera with a position and a target point
        public Camera(Vector3D<float> position, Vector3D<float> target)
        {
            Position = position;

            // Calculate the direction vector from the position to the target
            Vector3D<float> direction = Vector3D.Normalize(target - position);
            pitch = (float)(Math.Asin(direction.Y) * 180.0 / Math.PI);
            yaw = (float)(Math.Atan2(direction.Z, direction.X) * 180.0 / Math.PI);

            UpdateVectors();
        }

        // Specified movement methods for the camera
        public void MoveForward(float deltaTime) => Position += Front * MoveSpeed * deltaTime;
        public void MoveBackward(float deltaTime) => Position -= Front * MoveSpeed * deltaTime;
        public void MoveRight(float deltaTime) => Position += Right * MoveSpeed * deltaTime;
        public void MoveLeft(float deltaTime) => Position -= Right * MoveSpeed * deltaTime;
        public void MoveUp(float deltaTime) => Position += Up * MoveSpeed * deltaTime;
        public void MoveDown(float deltaTime) => Position -= Up * MoveSpeed * deltaTime;

        // Processes mouse movement to adjust the camera's yaw and pitch
        public void ProcessMouseMovement(Vector2 mousePos)
        {
            if (firstMouseMove)
            {
                lastMousePos = mousePos;
                firstMouseMove = false;
                return;
            }

            float xOffset = (mousePos.X - lastMousePos.X) * MouseSensitivity;
            float yOffset = (lastMousePos.Y - mousePos.Y) * MouseSensitivity; // Inverted Y-axis for mouse movement
            lastMousePos = mousePos;

            yaw += xOffset;
            pitch = Math.Clamp(pitch + yOffset, -89.0f, 89.0f);

            UpdateVectors();
        }

        // Get the view matrix for the camera. World coordinates -> Camera coordinates
        public Matrix4X4<float> GetViewMatrix()
        {
            return Matrix4X4.CreateLookAt(Position, Position + Front, Up);
        }

        // Updating the camera's vectors based on the angles
        private void UpdateVectors()
        {
            float yawRad = (float)(yaw * Math.PI / 180.0);
            float pitchRad = (float)(pitch * Math.PI / 180.0);

            Vector3D<float> front = new Vector3D<float>(
                (float)(Math.Cos(yawRad) * Math.Cos(pitchRad)),
                (float)Math.Sin(pitchRad),
                (float)(Math.Sin(yawRad) * Math.Cos(pitchRad))
            );

            Front = Vector3D.Normalize(front);
            Right = Vector3D.Normalize(Vector3D.Cross(Front, Vector3D<float>.UnitY));
            Up = Vector3D.Normalize(Vector3D.Cross(Right, Front));
        }
    }
}
