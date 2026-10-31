using Silk.NET.Maths;
using System;

namespace Grafika_Hazi
{
    // This class is responsible for controlling the rotation of the Rubik's cube slices
    public class RubikController
    {
        private float currentAngle = 0f;
        private float targetAngle = 0f;
        private const float AnimationSpeed = 3.5f;

        // 90 degrees rotation in the clockwise direction
        public void RotateClockwise()
        {
            targetAngle += (float)(Math.PI / 2.0);
        }

        // 90 degrees rotation in the counter-clockwise direction
        public void RotateCounterClockwise()
        {
            targetAngle -= (float)(Math.PI / 2.0);
        }

        public void Update(double deltaTime)
        {
            if (Math.Abs(currentAngle - targetAngle) < 0.0001f)
            {
                currentAngle = targetAngle;
                return;
            }

            float step = AnimationSpeed * (float)deltaTime;

            if (targetAngle > currentAngle)
            {
                currentAngle = Math.Min(currentAngle + step, targetAngle);
            }
            else
            {
                currentAngle = Math.Max(currentAngle - step, targetAngle);
            }
        }

        public Matrix4X4<float> GetSliceRotationMatrix(RubikPiece piece)
        {
            // The upper slice (GridY == 1) gets the rotation matrix applied, while the other slices remain unaffected
            if (piece.GridY == 1)
            {
                return Matrix4X4.CreateRotationY(currentAngle);
            }

            return Matrix4X4<float>.Identity;
        }
    }
}