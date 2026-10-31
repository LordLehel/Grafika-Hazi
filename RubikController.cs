using Silk.NET.Maths;
using System;

namespace Grafika_Hazi
{
    public enum SliceAxis
    {
        RowY, // Rows (Y axis rotation)
        ColX, // Columns (X axis rotation)
        SliceZ // Slices (Z axis rotation)
    }

    // This class is responsible for controlling the rotation of the Rubik's cube slices
    public class RubikController
    {
        private readonly RubikCube cube;
        private readonly Random random = new();

        public SliceAxis SelectedAxis { get; private set; } = SliceAxis.RowY;
        public int SelectedIndex { get; private set; } = 1; // 1, 0, -1 for the three slices along the selected axis

        public bool IsDepthMode { get; private set; } = false;

        private bool isAnimating = false;
        private float currentAngle = 0f;
        private float targetAngle = 0f;
        private int currentDirection = 1;
        private const float AnimationSpeed = 5.0f;

        public bool IsExploding { get; private set; } = false;
        public float ExplosionProgress { get; private set; } = 0f;
        private bool isScrambled = false;

        public RubikController(RubikCube cube)
        {
            this.cube = cube;
        }

        // Toggles between Z-axis and X-axis
        public void ToggleDepthMode()
        {
            if (isAnimating || IsExploding) return;
            IsDepthMode = !IsDepthMode;

            // If a slice is currently selected, switch the axis accordingly
            if (SelectedAxis == SliceAxis.ColX || SelectedAxis == SliceAxis.SliceZ)
            {
                SelectedAxis = IsDepthMode ? SliceAxis.SliceZ : SliceAxis.ColX;
            }
        }

        // 1, 2, 3 - select row (1-U, 2-M, 3-D)
        public void SelectRow(int rowNumber)
        {
            if (isAnimating) return;
            SelectedAxis = SliceAxis.RowY;
            SelectedIndex = 2 - rowNumber; // 1 -> 1, 2 -> 0, 3 -> -1
        }

        // 4, 5, 6 - select column (4-L, 5-M, 6-R)
        public void SelectColumn(int colNumber)
        {
            if (isAnimating) return;
            SelectedAxis = IsDepthMode ? SliceAxis.SliceZ : SliceAxis.ColX;
            SelectedIndex = colNumber - 2; // 1 -> -1, 2 -> 0, 3 -> 1
        }

        // Q button: Row -> left (-Y), Column -> down (+X)
        public void RotateNegative()
        {
            if (isAnimating) return;
            int dir = (SelectedAxis == SliceAxis.RowY) ? -1 : 1;
            StartRotation(dir);
        }

        // E button: Row -> right (+Y), Column -> up (-X)
        public void RotatePositive()
        {
            if (isAnimating) return;
            int dir = (SelectedAxis == SliceAxis.RowY) ? 1 : -1;
            StartRotation(dir);
        }

        // Rotates a slice of the cube for shuffling
        private void StartRotation(int direction)
        {
            isAnimating = true;
            currentDirection = direction;
            currentAngle = 0f;
            targetAngle = direction * (float)(Math.PI / 2.0);
        }

        // Randomly scrambles the Rubik's cube by applying a series of random rotations
        public void Scramble(int moveCount = 25)
        {
            if (isAnimating) return;

            SliceAxis[] axes = { SliceAxis.RowY, SliceAxis.ColX, SliceAxis.SliceZ };
            int[] indices = { -1, 0, 1 };
            int[] dirs = { -1, 1 };

            for (int i = 0; i < moveCount; i++)
            {
                SliceAxis axis = axes[random.Next(axes.Length)];
                int index = indices[random.Next(indices.Length)];
                int dir = dirs[random.Next(dirs.Length)];

                ApplyMove(axis, index, dir);
            }
            isScrambled = true;
        }

        // Updates the animation state of the cube rotation based on the elapsed time
        public void Update(double deltaTime)
        {
            if (IsExploding)
            {
                ExplosionProgress += (float)deltaTime * 3.5f;
                return;
            }

            if (!isAnimating) return;

            float step = AnimationSpeed * (float)deltaTime;

            if (targetAngle > 0)
            {
                currentAngle += step;
                if (currentAngle >= targetAngle) FinishAnimation();
            }
            else
            {
                currentAngle -= step;
                if (currentAngle <= targetAngle) FinishAnimation();
            }
        }

        // Completes the rotation animation and applies the final move to the cube
        private void FinishAnimation()
        {
            currentAngle = 0f;
            isAnimating = false;
            ApplyMove(SelectedAxis, SelectedIndex, currentDirection);

            // Check if the cube is solved after the move
            if (isScrambled && CheckSolved())
            {
                IsExploding = true;
                isScrambled = false;
            }
        }

        // Checks if the cube is solved by comparing the current model matrices of the pieces with their original model matrices
        private bool CheckSolved()
        {
            foreach (var piece in cube.Pieces)
            {
                float diff = Math.Abs(piece.ModelMatrix.M41 - piece.OriginalModelMatrix.M41) +
                             Math.Abs(piece.ModelMatrix.M42 - piece.OriginalModelMatrix.M42) +
                             Math.Abs(piece.ModelMatrix.M43 - piece.OriginalModelMatrix.M43) +
                             Math.Abs(piece.ModelMatrix.M11 - piece.OriginalModelMatrix.M11) +
                             Math.Abs(piece.ModelMatrix.M22 - piece.OriginalModelMatrix.M22) +
                             Math.Abs(piece.ModelMatrix.M33 - piece.OriginalModelMatrix.M33);

                if (diff > 0.05f) return false;
            }
            return true;
        }

        // Applies the rotation to the cube's pieces based on the selected axis, index, and direction
        private void ApplyMove(SliceAxis axis, int index, int dir)
        {
            float angle = dir * (float)(Math.PI / 2.0);
            Matrix4X4<float> rotMatrix = axis switch
            {
                SliceAxis.RowY => Matrix4X4.CreateRotationY(angle),
                SliceAxis.ColX => Matrix4X4.CreateRotationX(angle),
                SliceAxis.SliceZ => Matrix4X4.CreateRotationZ(angle),
                _ => Matrix4X4<float>.Identity
            };

            foreach (var piece in cube.Pieces)
            {
                if (IsPieceInSlice(piece, axis, index))
                {
                    // Apply the rotation to the piece's model matrix
                    piece.ModelMatrix = piece.ModelMatrix * rotMatrix;

                    int x = piece.GridX;
                    int y = piece.GridY;
                    int z = piece.GridZ;

                    // Update the grid coordinates based on the rotation axis and direction
                    if (axis == SliceAxis.RowY)
                    {
                        piece.GridX = dir == 1 ? z : -z;
                        piece.GridZ = dir == 1 ? -x : x;
                    }
                    else if (axis == SliceAxis.ColX)
                    {
                        piece.GridY = dir == 1 ? -z : z;
                        piece.GridZ = dir == 1 ? y : -y;
                    }
                    else if (axis == SliceAxis.SliceZ)
                    {
                        piece.GridX = dir == 1 ? -y : y;
                        piece.GridY = dir == 1 ? x : -x;
                    }
                }
            }
        }

        // Sets the selected slice based on the axis and index
        public bool IsPieceSelected(RubikPiece piece)
        {
            // If the cube is exploding, no piece should be considered selected
            if (IsExploding) return false;
            return IsPieceSelected(piece, SelectedAxis, SelectedIndex);
        }

        // Checks if a given piece is part of the currently selected slice based on the axis and index
        private static bool IsPieceSelected(RubikPiece piece, SliceAxis axis, int index)
        {
            return axis switch
            {
                SliceAxis.RowY => piece.GridY == index,
                SliceAxis.ColX => piece.GridX == index,
                SliceAxis.SliceZ => piece.GridZ == index,
                _ => false
            };
        }

        // Checks if a given piece is part of the currently selected slice based on the axis and index
        private static bool IsPieceInSlice(RubikPiece piece, SliceAxis axis, int index)
        {
            return axis switch
            {
                SliceAxis.RowY => piece.GridY == index,
                SliceAxis.ColX => piece.GridX == index,
                SliceAxis.SliceZ => piece.GridZ == index,
                _ => false
            };
        }

        // Returns the rotation matrix for a piece if it is part of the currently animating slice
        public Matrix4X4<float> GetSliceRotationMatrix(RubikPiece piece)
        {
            if (isAnimating && IsPieceInSlice(piece, SelectedAxis, SelectedIndex))
            {
                return SelectedAxis switch
                {
                    SliceAxis.RowY => Matrix4X4.CreateRotationY(currentAngle),
                    SliceAxis.ColX => Matrix4X4.CreateRotationX(currentAngle),
                    SliceAxis.SliceZ => Matrix4X4.CreateRotationZ(currentAngle),
                    _ => Matrix4X4<float>.Identity
                };
            }

            return Matrix4X4<float>.Identity;
        }
    }
}