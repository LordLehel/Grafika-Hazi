using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GrafikaSzeminarium
{
    internal class ModelObjectDescriptor:IDisposable
    {
        private bool disposedValue;

        public uint Vao { get; private set; }
        public uint Vertices { get; private set; }
        public uint Colors { get; private set; }
        public uint Indices { get; private set; }
        public uint IndexArrayLength { get; private set; }

        private GL Gl;

        // Oldal letrehozasa
        public unsafe static ModelObjectDescriptor CreatePlank(GL Gl, bool smoothNormals, float halfAngle)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            // a normalis szamolasa: (0,0,1) elforgatva az Y tengely korul halfAngle szoggel
            float nxRight = smoothNormals ? MathF.Sin(halfAngle) : 0f;
            float nxLeft = -nxRight;
            float nz = smoothNormals ? MathF.Cos(halfAngle) : 1f;

            // csucsonkent: pozicio (3 float) + normalis (3 float)
            // a sorrend a +Z fele nezve oramutato jarasaval ellentetes, igy ez a front face
            float[] vertexArray = new float[] {
                -0.5f,  1f, 0f, nxLeft,  0f, nz,   // bal felso
                -0.5f, -1f, 0f, nxLeft,  0f, nz,   // bal also
                 0.5f, -1f, 0f, nxRight, 0f, nz,   // jobb also
                 0.5f,  1f, 0f, nxRight, 0f, nz,   // jobb felso

                -0.5f,  1f, 0f, -nxLeft,  0f, -nz,   // bal felso (hatso oldal)
                -0.5f, -1f, 0f, -nxLeft,  0f, -nz,   // bal also (hatso oldal)
                 0.5f, -1f, 0f, -nxRight, 0f, -nz,   // jobb also (hatso oldal)
                 0.5f,  1f, 0f, -nxRight, 0f, -nz,   // jobb felso (hatso oldal)
            };

            // egyszinu lapok
            float[] colorArray = new float[] {
                0.8f, 0.5f, 0.2f, 1f,
                0.8f, 0.5f, 0.2f, 1f,
                0.8f, 0.5f, 0.2f, 1f,
                0.8f, 0.5f, 0.2f, 1f,

                0.6f, 0.35f, 0.15f, 1f,
                0.6f, 0.35f, 0.15f, 1f,
                0.6f, 0.35f, 0.15f, 1f,
                0.6f, 0.35f, 0.15f, 1f,
            };

            // ket haromszog alkotja a teglalapot elol, ket masik hatul
            uint[] indexArray = new uint[] {
                0, 1, 2,
                0, 2, 3,

                4, 6, 5,
                4, 7, 6
            };

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            // 0. attributum a pozicio, 2. a normalis
            uint offsetPos = 0;
            uint offsetNormals = offsetPos + 3 * sizeof(float);
            uint vertexSize = offsetNormals + 3 * sizeof(float);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
            Gl.EnableVertexAttribArray(2);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            // 1. attributum a szin
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);

            return new ModelObjectDescriptor() { Vao = vao, Vertices = vertices, Colors = colors, Indices = indices, IndexArrayLength = (uint)indexArray.Length, Gl = Gl };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null


                // always unbound the vertex buffer first, so no halfway results are displayed by accident
                Gl.DeleteBuffer(Vertices);
                Gl.DeleteBuffer(Colors);
                Gl.DeleteBuffer(Indices);
                Gl.DeleteVertexArray(Vao);

                disposedValue = true;
            }
        }

        ~ModelObjectDescriptor()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
