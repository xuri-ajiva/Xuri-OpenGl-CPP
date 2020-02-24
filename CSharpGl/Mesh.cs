using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSharpGL;
using GlmNet;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform.Windows;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;
using static CSharpGl.exth;

namespace CSharpGl {
    struct BMFMaterial {
        public Vector3 diffuse;
        public Vector3 specular;
        public Vector3 emissive;
        public float   shininess;
    };

    struct Material {
        public BMFMaterial material;
        public int         diffuseMap;
        public int         normalMap;
    };

    class Mesh {

        public Mesh(IntPtr data,     UInt64 numVertices, List<uint> indices, ulong numIndices,
            Material       material, Shader shader,      VertexMode mode) {
            this.vertexMode = mode;
            this.material   = material;
            this.shader     = shader;
            this.numIndices = numIndices;

            IntPtr ptr = IntPtr.Zero;

            this.vertexBuffer = new VertexBuffer( data, numVertices, mode );

            PrintStatus( 'V', vertexBuffer.GET_BUFFER_ID(), ". vertexBuffer", 3, MESSAGE_COLOR );
            uint[] buffer = indices.ToArray();

            unsafe {
                fixed (uint* p = buffer) {
                    ptr              = (IntPtr) p;
                    this.indexBuffer = new IndexBuffer( ptr, numIndices, sizeof(int) );
                }
            }

            PrintStatus( 'I', indexBuffer.GET_BUFFER_ID(), ". indexBuffer", 5, MESSAGE_COLOR );

            this.diffuseLocation      = GL.GetUniformLocation( shader.GetShaderID(), "u_material.diffuse" );
            this.specularLocation     = GL.GetUniformLocation( shader.GetShaderID(), "u_material.specular" );
            this.emissiveLocation     = GL.GetUniformLocation( shader.GetShaderID(), "u_material.emissive" );
            this.shininessLocation    = GL.GetUniformLocation( shader.GetShaderID(), "u_material.shininess" );
            this.diffuseMapLocation   = GL.GetUniformLocation( shader.GetShaderID(), "u_diffuse_map" );
            this.normalMapLocation    = GL.GetUniformLocation( shader.GetShaderID(), "u_normal_map" );
            this.useNormalMapLocation = GL.GetUniformLocation( shader.GetShaderID(), "u_use_normal_map" );
            this.useTexturesLocation  = GL.GetUniformLocation( shader.GetShaderID(), "u_use_textures" );
        }


        ~Mesh() {
            this.vertexBuffer = default;
            this.indexBuffer  = default;
        }

        public void Render() {
            this.vertexBuffer.BIND();
            this.indexBuffer.BIND();
            GL.Uniform3( this.diffuseLocation,  this.material.material.diffuse );
            GL.Uniform3( this.specularLocation, this.material.material.specular );
            GL.Uniform3( this.emissiveLocation, this.material.material.emissive );
            GL.Uniform1( this.shininessLocation, this.material.material.shininess );

            if ( this.vertexMode == VertexMode.TextureOnly || this.vertexMode == VertexMode.TextureAndNormal ) {
                GL.BindTexture( TextureTarget.Texture2D, this.material.diffuseMap );
                GL.Uniform1( this.diffuseMapLocation,  0 );
                GL.Uniform1( this.useTexturesLocation, 1 ); //tell shader to use textures
            }
            else {
                GL.Uniform1( this.useTexturesLocation, 0 );
            }

            if ( this.vertexMode == VertexMode.TextureAndNormal ) {
                GL.ActiveTexture( TextureUnit.Texture1 );
                GL.BindTexture( TextureTarget.Texture2D, this.material.normalMap );
                GL.ActiveTexture( TextureUnit.Texture0 );
                GL.Uniform1( this.normalMapLocation,    1 );
                GL.Uniform1( this.useNormalMapLocation, 1 ); //tell shader to use normalMap
            }
            else {
                GL.Uniform1( this.useNormalMapLocation, 0 );
            }

            GL.DrawElements( PrimitiveType.Triangles, (int) this.numIndices, DrawElementsType.UnsignedInt, 0 );
            //TODO: check GL.DrawElements( GL_TRIANGLES,            this.numIndices, GL_UNSIGNED_INT,              0 );
            //vertexBuffer.UNBIND();
            //indexBuffer.UNBIND();
        }


        private VertexMode   vertexMode;
        private VertexBuffer vertexBuffer;
        private IndexBuffer  indexBuffer;
        private Shader       shader;
        private Material     material;
        private ulong        numIndices = 0;
        private int          diffuseLocation;
        private int          specularLocation;
        private int          emissiveLocation;
        private int          shininessLocation;
        private int          diffuseMapLocation;
        private int          normalMapLocation;
        private int          useTexturesLocation;
        private int          useNormalMapLocation;
    };

    class Model {
        private FileStream   fs = default;
        private BinaryReader br = default;
        private BinaryWriter bw = default;

        ulong sizeOfVertex;
        ulong numMaterials;
        ulong numMeshes;
        int   sizeMaterial;

        VertexMode vm;

        bool readFirstInfo() {
            if ( this.fs == null ) return false;

            this.br = new BinaryReader( this.fs );
            this.bw = new BinaryWriter( this.fs );

            this.sizeOfVertex = this.br.ReadUInt64();
            this.numMaterials = this.br.ReadUInt64();
            this.sizeMaterial = System.Runtime.InteropServices.Marshal.SizeOf( typeof(BMFMaterial) );

            if ( this.sizeOfVertex == (ulong) System.Runtime.InteropServices.Marshal.SizeOf( typeof(VertexTextureAndNormal) ) ) {
                this.vm = VertexMode.TextureAndNormal;
            }
            else if ( this.sizeOfVertex == (ulong) System.Runtime.InteropServices.Marshal.SizeOf( typeof(VertexTextureOnly) ) ) {
                this.vm = VertexMode.TextureOnly;
            }
            else if ( this.sizeOfVertex == (ulong) System.Runtime.InteropServices.Marshal.SizeOf( typeof(VertexMaterialOnly) ) ) {
                this.vm = VertexMode.MaterialOnly;
            }
            else {
                return false;

                throw new Exception( "No EnumCode" );
            }

            return true;
        }

        void LoadFile(string FileName) {
            this.fs?.Close();

            this.fs = File.Open( FileName, FileMode.Open );
            if ( !readFirstInfo() ) return;

            for ( UInt64 i = 0; i < this.numMaterials; i++ ) {
                byte[] buff = new byte[this.sizeMaterial];
                this.br.Read( buff, 0, this.sizeMaterial );
                Material material = fromBytesMatertal( buff );

                UInt64 diffuseMapNameLength = this.br.ReadUInt64();
                buff = new byte[diffuseMapNameLength];
                this.br.Read( buff, 0, (int) diffuseMapNameLength );
                string dm = Encoding.UTF8.GetString( buff );

                UInt64 normalMapNameLength = this.br.ReadUInt64();
                buff = new byte[normalMapNameLength];
                this.br.Read( buff, 0, (int) normalMapNameLength );
                string nm = Encoding.UTF8.GetString( buff );

                //todo: this.listView1.Items.Add( new ListViewItem( new[] { material.ToString(), "D: " + dm + "   N: " + nm } ) );
            }

            this.numMeshes = this.br.ReadUInt64();

            for ( UInt64 i = 0; i < this.numMeshes; i++ ) {
                UInt64 materialIndex = this.br.ReadUInt64();
                UInt64 numVertices   = this.br.ReadUInt64();
                UInt64 numIndices    = this.br.ReadUInt64();

                byte[] buff = new byte[numVertices * this.sizeOfVertex];
                this.br.Read( buff, 0, buff.Length );
                /*
                for ( UInt64 k = 0; k < numVertices; k++ ) {
                    var buff = new byte[sizeFoVertex];
                    b.Read( buff, 0, (int) sizeFoVertex );
                    //
                }*/

                buff = new byte[numIndices * sizeof(UInt32)];
                this.br.Read( buff, 0, buff.Length );
                /*
                for ( UInt64 g = 0; g < numIndices; g++ ) {
                    UInt32 index = b.ReadUInt32();
                    //indices.push_back(index);
                }*/

                //todo: this.listView2.Items.Add( new ListViewItem( new[] { numVertices.ToString(), numIndices.ToString(), materialIndex.ToString() } ) );
            }

            //todo:this.label3.Text = "MeshesCounr: " + this.numMeshes;

            //todo: FileViever_Validated( null, null );
        }

        public Model(string filename, Shader shader) {
            this.FileName = filename;
            this.Shader   = shader;
        }

        public void Init() {
            this.fs?.Close();

            this.fs = File.Open( this.FileName, FileMode.Open );
            if ( !readFirstInfo() ) return;

            for ( UInt64 i = 0; i < this.numMaterials; i++ ) {
                byte[] buff = new byte[this.sizeMaterial];
                this.br.Read( buff, 0, this.sizeMaterial );
                Material material = fromBytesMatertal( buff );

                UInt64 diffuseMapNameLength = this.br.ReadUInt64();
                buff = new byte[diffuseMapNameLength];
                this.br.Read( buff, 0, (int) diffuseMapNameLength );
                string dm = Encoding.UTF8.GetString( buff );
                PrintStatus( '+', (int) i, ( "diffuseMap: " + dm ), (int) ( diffuseMapNameLength > 0 ? PLUS_COLOR : MINUS_COLOR ), MESSAGE_COLOR );

                UInt64 normalMapNameLength = this.br.ReadUInt64();
                buff = new byte[normalMapNameLength];
                this.br.Read( buff, 0, (int) normalMapNameLength );
                string nm = Encoding.UTF8.GetString( buff );
                PrintStatus( '+', (int) i, ( "normalMap: " + nm ), (int) ( normalMapNameLength > 0 ? PLUS_COLOR : MINUS_COLOR ), MESSAGE_COLOR );

                //todo: this.listView1.Items.Add( new ListViewItem( new[] { material.ToString(), "D: " + dm + "   N: " + nm } ) );

                //assert(diffuseMapNameLength > 0);
                //assert(normalMapNameLength > 0);

                if ( this.vm == VertexMode.TextureOnly || this.vm == VertexMode.TextureAndNormal ) {
                    GL.GenTextures( 1, out material.diffuseMap );

                    if ( !File.Exists( dm ) ) {
                        Console.WriteLine( "file not exists ?" );
                        continue;
                    }

                    /*    //TODO:::
                   var    paths  = Path.GetFullPath( dm );
                   Bitmap bitmap = (Bitmap) Image.FromFile( dm );

                   //assert(textureBuffer);
                   //assert(material.diffuseMap);

                   GL.BindTexture( TextureTarget.Texture2D, material.diffuseMap );

                   GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear );
                   GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear );
                   GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS,     (int) TextureWrapMode.ClampToEdge );
                   GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT,     (int) TextureWrapMode.ClampToEdge );

                   BitmapData data = bitmap.LockBits( new Rectangle( 0, 0, bitmap.Width, bitmap.Height ), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb );

                   GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, data.Width, data.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data.Scan0 );

                   //todo: check GL.TexImage2D( GL_TEXTURE_2D, 0, GL_RGBA8, textureWidth, textureHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, textureBuffer );

                   bitmap.UnlockBits( data );

                   //todo: else { PrintError( '-', i, "textureBuffer or material.diffuseMap missing", MINUS_COLOR ); }  */
                }

                if ( this.vm == VertexMode.TextureAndNormal ) {
                    /*                                             ////TODO::::
                    GL.GenTextures( 1, out material.normalMap );
                    Bitmap bitmap = new Bitmap( nm );
                    //assert(textureBuffer);
                    //assert(material.normalMap);

                    GL.BindTexture( TextureTarget.Texture2D, material.normalMap );

                    GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear );
                    GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear );
                    GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS,     (int) TextureWrapMode.ClampToEdge );
                    GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT,     (int) TextureWrapMode.ClampToEdge );

                    BitmapData data = bitmap.LockBits( new Rectangle( 0, 0, bitmap.Width, bitmap.Height ), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb );

                    GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, data.Width, data.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data.Scan0 );

                    //todo: check GL.TexImage2D( GL_TEXTURE_2D, 0, GL_RGBA8, textureWidth, textureHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, textureBuffer );

                    bitmap.UnlockBits( data );

                    //todo: else { PrintError( '-', i, "textureBuffer or material.diffuseMap missing", MINUS_COLOR ); }    */
                }

                GL.BindTexture( TextureTarget.Texture2D, 0 );

                //todo: PrintStatus( 'C', i, ". Material", 11, MESSAGE_COLOR );
                this.materials.Add( material );
            }

            // mesh

            this.numMeshes       = this.br.ReadUInt64();
            this.meshes.Capacity = (int) this.numMeshes;

            for ( UInt64 i = 0; i < this.numMeshes; i++ ) {
                List<uint> indices = new List<uint>();

                UInt64 materialIndex = this.br.ReadUInt64();
                UInt64 numVertices   = this.br.ReadUInt64();
                UInt64 numIndices    = this.br.ReadUInt64();

                List<byte[]> _bits = new List<byte[]>( (int) numVertices );

                byte[]   buff   = new byte[numVertices * this.sizeOfVertex];
                GCHandle pinned = GCHandle.Alloc( buff, GCHandleType.Pinned );
                IntPtr   ptr    = pinned.AddrOfPinnedObject();

                this.br.Read( buff, 0, buff.Length );

                /*



                for ( UInt64 k = 0; k < numVertices; k++ ) {
                    var buff = new byte[this.sizeOfVertex];
                    this.br.Read( buff, 0, (int) this.sizeOfVertex );
                    _bits.Add( buff );
                    //
                }

                //////TODO:::: Add Converter from byte[] to all Vertex

                Type VertexT = default;
                Marshal.Copy( _bits[0], 0,ptr,0 );

                SomeFunction(ptr);
                switch (vm) {
                    case VertexMode.TextureAndNormal:
                        VertexT = typeof(VertexTextureAndNormal);
                        break;
                    case VertexMode.TextureOnly:
                        VertexT = typeof(VertexTextureOnly); break;
                    case VertexMode.MaterialOnly: 
                        VertexT = typeof(VertexMaterialOnly);
                        break;
                    default: 
                        //todo: PrintError( '-', -1, "unknown Vertex Struct!", MINUS_COLOR );
                        throw new ArgumentOutOfRangeException();
                }

                  */

                for ( UInt64 g = 0; g < numIndices && this.fs.Position < this.fs.Length; g++ ) {
                    UInt32 index = this.br.ReadUInt32();
                    indices.Add( index );
                }

                //todo:  this.listView2.Items.Add( new ListViewItem( new[] { numVertices.ToString(), numIndices.ToString(), materialIndex.ToString() } ) );

                Mesh mesh = new Mesh( ptr, numVertices, indices, numIndices, this.materials[(int) materialIndex], this.Shader, this.vm );
                PrintStatus( 'M', (int) i, ". Mesh*", 13, MESSAGE_COLOR );
                this.meshes.Add( mesh );

                pinned.Free();
            }
        }

        List<T> bitsToVertex <T>(List<byte[]> bits) {
            if ( typeof(T) != typeof(VertexTextureAndNormal) && typeof(T) != typeof(VertexMaterialOnly) && typeof(T) != typeof(VertexTextureOnly) ) {
                throw new ArgumentException( typeof(T).Name );
            }

            List<T> outref = new List<T>( bits.Count );

            foreach ( var bites in bits ) {
                T fin = default;

                if ( typeof(T) == typeof(VertexTextureAndNormal) ) {
                    fin = (T) ByteArrayToStruct( bites, 0, typeof(VertexTextureAndNormal) );
                }
                else if ( typeof(T) == typeof(VertexTextureOnly) ) {
                    fin = (T) ByteArrayToStruct( bites, 0, typeof(VertexTextureOnly) );
                }
                else if ( typeof(T) == typeof(VertexMaterialOnly) ) {
                    fin = (T) ByteArrayToStruct( bites, 0, typeof(VertexMaterialOnly) );
                }

                outref.Add( fin );
            }

            return outref;
        }

        public static Material fromBytesMatertal(byte[] bytes) {
            BMFMaterial s    = default;
            int         size = Marshal.SizeOf( s );
            IntPtr      ptr  = Marshal.AllocHGlobal( size );

            Marshal.Copy( bytes, 0, ptr, size );

            s = (BMFMaterial) Marshal.PtrToStructure( ptr, s.GetType() );

            var str = new Material() { material = s };

            Marshal.FreeHGlobal( ptr );

            return str;
        }

        /// <summary>
        /// Kopiert Daten aus einem Byte-Array in eine entsprechende Strukture (struct). Die Struktur muss ein sequenzeilles Layout besitzen. ( [StructLayout(LayoutKind.Sequential)]
        /// </summary>
        /// <param name="array">Das Byte-Array das die daten enthält</param>
        /// <param name="offset">Offset ab dem die Daten in die Struktur kopiert werden sollen.</param>
        /// <param name="structType">System.Type der Struktur</param>
        /// <returns></returns>
        static object ByteArrayToStruct(byte[] array, int offset, Type structType) {
            if ( structType.StructLayoutAttribute.Value != LayoutKind.Sequential )
                throw new ArgumentException( "structType ist keine Struktur oder nicht Sequentiell." );

            int size = Marshal.SizeOf( structType );

            byte[] tmp = new byte[size];

            if ( offset > 0 )
                Array.Copy( array, offset, tmp, 0, size );
            else
                tmp = array;

            GCHandle structHandle = GCHandle.Alloc( tmp, GCHandleType.Pinned );
            object   structure    = Marshal.PtrToStructure( structHandle.AddrOfPinnedObject(), structType );
            structHandle.Free();

            return structure;
        }

        public void Render() {
            foreach ( Mesh mesh in this.meshes ) {
                mesh.Render();
            }
        }

        ~Model() {
            this.meshes.Clear();
            this.meshes = null;
        }


        private Shader         Shader;
        private string         FileName  = "";
        private List<Mesh>     meshes    = new List<Mesh>();
        private List<Material> materials = new List<Material>();
    }

}
