using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using static GlmNet.glm;


namespace CSharpGL {
    struct IndexBuffer : IDisposable {

        int BUFFER_ID_;

        IndexBuffer(IntPtr data, int numIndex, int elementSize) {
            this.BUFFER_ID_ = GL.GenBuffer();
            GL.BindBuffer( BufferTarget.ElementArrayBuffer, this.BUFFER_ID_ );
            GL.BufferData( BufferTarget.ElementArrayBuffer, numIndex * elementSize, data, BufferUsageHint.StaticDraw );
        }

        void BIND() { GL.BindBuffer( BufferTarget.ElementArrayBuffer, this.BUFFER_ID_ ); }

        void UNBIND() { GL.BindBuffer( BufferTarget.ElementArrayBuffer, 0 ); }

        int GET_BUFFER_ID() { return this.BUFFER_ID_; }

        public void Dispose() { GL.DeleteBuffer( this.BUFFER_ID_ ); }
    }
}
