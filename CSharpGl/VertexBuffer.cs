using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace CSharpGl {
    struct VertexBuffer : IDisposable {
        int BUFFER_ID_;

        int VAO_;

        public VertexBuffer(IntPtr data, ulong numVertices, VertexMode mode) {
            VAO_ = GL.GenVertexArray();
            GL.BindVertexArray( VAO_ );

            int size = 0;

            unsafe {
                switch (mode) {
                    case VertexMode.TextureAndNormal:
                        size = sizeof(VertexTextureAndNormal);
                        break;

                    case VertexMode.TextureOnly:
                        size = sizeof(VertexTextureOnly);
                        break;

                    case VertexMode.MaterialOnly:

                        size = sizeof(VertexMaterialOnly);
                        break;

                    default: break;
                }
            }

            //bind all to vio **-
            BUFFER_ID_ = GL.GenBuffer();
            GL.BindBuffer( BufferTarget.ArrayBuffer, BUFFER_ID_ );
            GL.BufferData( BufferTarget.ArrayBuffer, (IntPtr) (numVertices *(uint) size), data, BufferUsageHint.StaticDraw );
            GL.EnableVertexAttribArray( 0 );
            GL.VertexAttribPointer( 0, 3, VertexAttribPointerType.Float, false, size, Marshal.OffsetOf( typeof(VertexMaterialOnly), "position" ) );
            GL.EnableVertexAttribArray( 1 );
            GL.VertexAttribPointer( 1, 3, VertexAttribPointerType.Float, false, size, Marshal.OffsetOf( typeof(VertexMaterialOnly), "normal" ) );

            if ( mode == VertexMode.TextureAndNormal ) {
                GL.EnableVertexAttribArray( 2 );
                GL.VertexAttribPointer( 2, 3, VertexAttribPointerType.Float, false, size, Marshal.OffsetOf( typeof(VertexTextureAndNormal), "tangent" ) );
                GL.EnableVertexAttribArray( 3 );
                GL.VertexAttribPointer( 3, 3, VertexAttribPointerType.Float, false, size, Marshal.OffsetOf( typeof(VertexTextureAndNormal), "textureCord" ) );
            }
            else if ( mode == VertexMode.TextureOnly ) {
                GL.EnableVertexAttribArray( 2 );
                GL.VertexAttribPointer( 3, 3, VertexAttribPointerType.Float, false, size, Marshal.OffsetOf( typeof(VertexTextureOnly), "textureCord" ) );
            }

            //bind all to vio -**
            GL.BindVertexArray( 0 );
        }


        public void BIND() { GL.BindVertexArray( VAO_ ); }

        void UNBIND() { GL.BindVertexArray( 0 ); }

        public int GET_BUFFER_ID() { return BUFFER_ID_; }

        public void Dispose() { GL.DeleteBuffer( this.BUFFER_ID_ ); }
    }
}
