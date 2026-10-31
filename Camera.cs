using Silk.NET.Maths;
using System;

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

        // Updates the camera's front, right, and up vectors based on the current yaw and pitch values
        public void ModifyDirection(float yawOffset, float pitchOffset)
        {
            yaw += yawOffset;
            pitch += pitchOffset;

            // Disable pitch rotation beyond vertical
            pitch = Math.Clamp(pitch, -89f, 89f);

            UpdateVectors();
        }

        // Moves the camera in a specified direction
        public void Move(Vector3D<float> direction, float speed)
        {
            Position += direction * speed;
        }

        // Get the view matrix for the camera. World coordinates -> Camera coordinates
        public Matrix4X4<float> GetViewMatrix()
        {
            return Matrix4X4.CreateLookAt(Position, Position + Front, Up);
        }

        // Updating the camera's vectors based on the angles
        private void UpdateVectors()
        {
            Front = new Vector3D<float>(
                (float)(Math.Cos(yaw * Math.PI / 180) * Math.Cos(pitch * Math.PI / 180)),
                (float)(Math.Sin(pitch * Math.PI / 180)),
                (float)(Math.Sin(yaw * Math.PI / 180) * Math.Cos(pitch * Math.PI / 180))
            );
            Front = Vector3D.Normalize(Front);
            Right = Vector3D.Normalize(Vector3D.Cross(Front, Vector3D<float>.UnitY));
            Up = Vector3D.Normalize(Vector3D.Cross(Right, Front));
        }
    }
}
