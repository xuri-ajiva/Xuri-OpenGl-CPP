using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Assimp;

namespace ModelConverterGUI {
    public class ModelWorker {
        private string _fileaName = "";
        List<Mesh>     _meshes    = new List<Mesh>();
        List<Material> _materials = new List<Material>();

        void ProcessMesh(Assimp.Mesh mesh, Assimp.Scene scene, VertexMode mode) {
            Mesh m = new Mesh();

            for ( int i = 0; i < mesh.VertexCount; i++ ) {
                if ( mesh.HasVertices ) {
                    Vec3 vec3 = new Vec3( mesh.Vertices[i] );
                    m.positions.Add( vec3 );
                }
                else {
                    Console.WriteLine( "[" + i + "]: " + "No Vertices" );
                }

                if ( mesh.HasNormals ) {
                    Vec3 normal = new Vec3( mesh.Normals[i] );
                    m.normals.Add( normal );
                }
                else {
                    Console.WriteLine( "[" + i + "]: " + "No Normals" );
                }

                if ( mode == VertexMode.TextureAndNormal )
                    if ( mesh.HasTangentBasis ) {
                        Vec3 tangent = new Vec3( mesh.Tangents[i] );
                        m.tangents.Add( tangent );
                    }
                    else {
                        Console.WriteLine( "[" + i + "]: " + "No Tangents" );
                    }

                if ( mode == VertexMode.TextureAndNormal || mode == VertexMode.TextureOnly )
                    if ( mesh.HasTextureCoords( 0 ) ) {
                        Vec2 uv = new Vec2( mesh.TextureCoordinateChannels[0][i] );
                        m.uvs.Add( uv );
                    }
                    else {
                        Console.WriteLine( "[" + i + "]: " + "No TextureCoordinate" );
                    }
            }

            foreach ( Face f in mesh.Faces ) {
                for ( int j = 0;
                    j < f.IndexCount;
                    j++ ) {
                    m.indices.Add( f.Indices[j] );
                }
            }

            m.materialIndex = mesh.MaterialIndex;
            this._meshes.Add( m );
        }

        void ProcessNode(Node node, Scene scene, VertexMode mode) {
            for ( int i = 0; i < node.MeshCount; i++ ) {
                Assimp.Mesh mesh = scene.Meshes[node.MeshIndices[i]];
                ProcessMesh( mesh, scene, mode );
            }

            for ( int i = 0; i < node.ChildCount; i++ ) {
                ProcessNode( node.Children[i], scene, mode );
            }
        }

        void ProcessMaterials(Scene scene, string directoryName, VertexMode mode) {
            for ( int i = 0; i < scene.MaterialCount; i++ ) {
                Material        mat      = new Material();
                Assimp.Material material = scene.Materials[i];

                var diffuse = material.ColorDiffuse;
                mat.diffuse = new Vec3( diffuse.R, diffuse.G, diffuse.B );

                var specular = material.ColorSpecular;
                mat.specular = new Vec3( specular.R, specular.G, specular.B );

                var emissive = material.ColorEmissive;
                mat.emissive = new Vec3( emissive.R, emissive.G, emissive.B );

                float shininess = material.Shininess;
                mat.shininess = shininess;

                float shininessStrength = material.ShininessStrength;

                mat.specular.x *= shininessStrength;
                mat.specular.y *= shininessStrength;
                mat.specular.z *= shininessStrength;

                int numDiffuseMaps = material.GetMaterialTextureCount( TextureType.Diffuse );
                int numNormalMaps  = material.GetMaterialTextureCount( TextureType.Normals );

                if ( numDiffuseMaps > 0 ) {
                    material.GetMaterialTexture( TextureType.Diffuse, 0, out var diffuseMapName );
                    Console.WriteLine( diffuseMapName.FilePath );

                    if ( !File.Exists( diffuseMapName.FilePath ) && ( mode == VertexMode.TextureAndNormal || mode == VertexMode.TextureOnly ) ) {
                        var a = new ModelWorkerEventArgs() { Args = diffuseMapName.FilePath, EventType = ModelWorkerEventArgs.EventArgsType.TextureNotExists, Context = directoryName };
                        this.OnActionCallback?.Invoke( this, a );
                        diffuseMapName.FilePath = a.Args.Replace( directoryName, "" );
                    }

                    mat.diffuseMapName = diffuseMapName.FilePath;
                }

                if ( numNormalMaps > 0 ) {
                    material.GetMaterialTexture( TextureType.Normals, 0, out var normalMapName );
                    Console.WriteLine( normalMapName.FilePath );

                    if ( !File.Exists( normalMapName.FilePath ) && mode == VertexMode.TextureAndNormal ) {
                        var a = new ModelWorkerEventArgs() { Args = normalMapName.FilePath, EventType = ModelWorkerEventArgs.EventArgsType.TextureNotExists, Context = directoryName };
                        this.OnActionCallback?.Invoke( this, a );
                        normalMapName.FilePath = a.Args.Replace( directoryName, "" );
                        ;
                    }

                    mat.normalMapName = normalMapName.FilePath;
                }

                this._materials.Add( mat );
            }
        }

        public int main(string fileName, VertexMode mode) {
            string directory = Path.GetDirectoryName( fileName );
            Int64  hf        = 1;

            AssimpContext importer = new AssimpContext();
            Scene         scene    = null;

            try {
                scene = importer.ImportFile( fileName, PostProcessSteps.PreTransformVertices | PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals | PostProcessSteps.OptimizeMeshes | PostProcessSteps.OptimizeGraph | PostProcessSteps.JoinIdenticalVertices | PostProcessSteps.ImproveCacheLocality | PostProcessSteps.CalculateTangentSpace );
            } catch (Exception ex) {
                Console.WriteLine( "Error while loading model with assimp: " + ex.Message );
            }

            if ( scene == null || scene.SceneFlags == SceneFlags.Incomplete || scene.RootNode == null ) {
                Console.WriteLine( "Error while loading model with assimp: " + ( scene == null ? "null" : scene.SceneFlags.ToString() ) );

                return 1;
            }

            UInt64 sizeOfVertex = 0;

            switch (mode) {
                case VertexMode.TextureAndNormal:
                    sizeOfVertex = (ulong) System.Runtime.InteropServices.Marshal.SizeOf( typeof(VertexTextureAndNormal) );

                    break;

                case VertexMode.TextureOnly:
                    sizeOfVertex = (ulong) System.Runtime.InteropServices.Marshal.SizeOf( typeof(VertexTextureOnly) );

                    break;

                case VertexMode.MaterialOnly:
                    sizeOfVertex = (ulong) System.Runtime.InteropServices.Marshal.SizeOf( typeof(VertexMaterialOnly) );

                    break;

                default:
                    Console.WriteLine( "No EnumCode" );

                    return -1;
            }

            ProcessMaterials( scene, Path.GetDirectoryName( fileName )+ "\\", mode );
            ProcessNode( scene.RootNode, scene, mode );

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension( fileName );
            string outputFilename           = directory + "\\" + fileNameWithoutExtension + ".bmf";
            Console.WriteLine( "OutputFile: "                                             + outputFilename );

            File.Delete( outputFilename );
            var fex = File.Open( outputFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite );

            using ( BinaryWriter br = new BinaryWriter( fex ) ) {
                Console.WriteLine( "Writing bmf file..." );
                br.Write( sizeOfVertex );

                Console.WriteLine( "Writing Materials:" );

                // Materials
                UInt64 numMaterials = (ulong) this._materials.Count;
                br.Write( numMaterials );

                foreach ( Material material in _materials ) {
                    br.Write( getBytes( material ) );

                    const string pathPrefix           = "models/";
                    UInt64       diffuesMapNameLength = 0;
                    UInt64       normalMapNameLength  = 0;

                    // Diffuse map
                    if ( mode == VertexMode.TextureOnly || mode == VertexMode.TextureAndNormal ) {
                        if ( !string.IsNullOrEmpty( material.diffuseMapName ) )
                            diffuesMapNameLength = (UInt64) ( material.diffuseMapName.Length + pathPrefix.Length );
                    }

                    br.Write( diffuesMapNameLength );

                    if ( diffuesMapNameLength > 0 && ( mode == VertexMode.TextureOnly || mode == VertexMode.TextureAndNormal ) ) {
                        br.Write( Encoding.UTF8.GetBytes( pathPrefix + material.diffuseMapName ) );
                        Console.WriteLine( pathPrefix + material.diffuseMapName );
                    }

                    // Normal map
                    if ( mode == VertexMode.TextureAndNormal ) {
                        if ( !string.IsNullOrEmpty( material.normalMapName ) )
                            normalMapNameLength = (UInt64) ( material.normalMapName.Length + pathPrefix.Length );
                    }

                    br.Write( normalMapNameLength );

                    if ( normalMapNameLength > 0 && mode == VertexMode.TextureAndNormal ) {
                        br.Write( Encoding.UTF8.GetBytes( pathPrefix + material.normalMapName ) );
                        Console.WriteLine( pathPrefix + material.normalMapName );
                    }

                    Console.WriteLine( "   - [" + ( hf++ ) + "/" + this._materials.Count + "]: Material" );
                }

                hf = 1;
                Console.WriteLine( "Writing Meshes : " );
                // Meshes
                UInt64 numMeshes = (UInt64) this._meshes.Count;
                br.Write( numMeshes );

                foreach ( Mesh mesh in _meshes ) {
                    UInt64 numVertices   = (UInt64) mesh.positions.Count;
                    UInt64 numIndices    = (UInt64) mesh.indices.Count;
                    UInt64 materialIndex = (UInt64) mesh.materialIndex;

                    br.Write( materialIndex );
                    br.Write( numVertices );
                    br.Write( numIndices );

                    for ( UInt64 i = 0; i < numVertices; i++ ) {
                        br.Write( mesh.positions[(int) i].x );
                        br.Write( mesh.positions[(int) i].y );
                        br.Write( mesh.positions[(int) i].z );

                        if ( mesh.normals.Count > (int) i ) {
                            br.Write( mesh.normals[(int) i].x );
                            br.Write( mesh.normals[(int) i].y );
                            br.Write( mesh.normals[(int) i].z );
                        }
                        //else {
                        //    br.Write( 0.0F );
                        //    br.Write( 0.0F );
                        //    br.Write( 0.0F );
                        //}

                        if ( mode == VertexMode.TextureAndNormal ) {
                            if ( mesh.tangents.Count > (int) i ) {
                                br.Write( mesh.tangents[(int) i].x );
                                br.Write( mesh.tangents[(int) i].y );
                                br.Write( mesh.tangents[(int) i].z );
                            }

                            //else {
                            //    br.Write( 0.0F );
                            //    br.Write( 0.0F );
                            //    br.Write( 0.0F );
                            //}
                        }

                        if ( mode == VertexMode.TextureAndNormal || mode == VertexMode.TextureOnly ) {
                            if ( mesh.uvs.Count > (int) i ) {
                                br.Write( mesh.uvs[(int) i].x );
                                br.Write( mesh.uvs[(int) i].y );
                            }

                            //else {
                            //    br.Write( 0.0F );
                            //    br.Write( 0.0F );
                            //}
                        }
                    }

                    for ( UInt64 i = 0; i < numIndices; i++ ) {
                        br.Write( mesh.indices[(int) i] );
                    }

                    Console.WriteLine( "   - [" + ( hf++ ) + "/" + this._meshes.Count + "]: Mesh" );
                }
            }

            fex.Close();
            Console.WriteLine( "Finished!" );

            this._meshes.Clear();
            this._materials.Clear();

            return 0;
        }

        static byte[] getBytes(Material material) {
            int    size = Marshal.SizeOf( material );
            int    s    = Marshal.SizeOf( typeof(BMFMaterial) );
            byte[] arr  = new byte[size];

            try {
                IntPtr ptr = Marshal.AllocHGlobal( size );
                Marshal.StructureToPtr( material, ptr, false );
                Marshal.Copy( ptr, arr, 0, s );
                Marshal.FreeHGlobal( ptr );
            } catch (Exception e) {
                Console.WriteLine( e.Message );
            }

            return arr.Take( s ).ToArray();

            //return material.GetBytes();
        }

        public static Material fromBytesMatertal(byte[] bytes) {
            BMFMaterial s    = default;
            Material    str  = default;
            int         size = Marshal.SizeOf( s );
            IntPtr      ptr  = Marshal.AllocHGlobal( size );

            Marshal.Copy( bytes, 0, ptr, size );

            s = (BMFMaterial) Marshal.PtrToStructure( ptr, s.GetType() );

            str = new Material() { diffuse = s.diffuse, shininess = s.shininess, specular = s.specular, emissive = s.emissive };

            Marshal.FreeHGlobal( ptr );

            return str;
        }

        static byte[] getBytesT <T>(T value) {
            int    size = Marshal.SizeOf( value );
            byte[] arr  = new byte[size];

            try {
                IntPtr ptr = Marshal.AllocHGlobal( size );
                Marshal.StructureToPtr( value, ptr, false );
                Marshal.Copy( ptr, arr, 0, size );
                Marshal.FreeHGlobal( ptr );
            } catch (Exception e) {
                Console.WriteLine( e.Message );
            }

            return arr;
        }

        static T fromBytes <T>(byte[] arr) {
            T      str  = default;
            int    size = Marshal.SizeOf( str );
            IntPtr ptr  = Marshal.AllocHGlobal( size );

            Marshal.Copy( arr, 0, ptr, size );

            str = (T) Marshal.PtrToStructure( ptr, str.GetType() );
            Marshal.FreeHGlobal( ptr );

            return str;
        }

        public event Action<object, ModelWorkerEventArgs> OnActionCallback;

        /// <inheritdoc />
        public class ModelWorkerEventArgs : EventArgs {

            public enum EventArgsType {
                TextureNotExists,
            }

            /// <summary>
            /// Modify This value to your fittings
            /// </summary>
            public string Args;

            /// <summary>
            /// For example The Path where the model is located
            /// </summary>
            public string Context;

            public EventArgsType EventType;
        }

    }


    #region Structe

    public struct Material {
        public Vec3   diffuse;
        public Vec3   specular;
        public Vec3   emissive;
        public float  shininess;
        public string diffuseMapName;
        public string normalMapName;

        public byte[] GetBytes() {
            int    sP  = sizeof(Single) * 3;
            byte[] arr = new byte[3 * sP + sizeof(float)];
            Array.Copy( this.diffuse.GetBytes(),                 0, arr, sP * 0,                 sP );
            Array.Copy( this.specular.GetBytes(),                0, arr, sP * 1,                 sP );
            Array.Copy( this.emissive.GetBytes(),                0, arr, sP * 2,                 sP );
            Array.Copy( BitConverter.GetBytes( this.shininess ), 0, arr, sP * 2 + sizeof(float), sizeof(float) );

            return arr;
        }

        #region Overrides of ValueType

        /// <inheritdoc />
        public override string ToString() {
            var str = "";
            str += "["                                       + this.diffuse.ToString();
            str += ", "                                      + this.specular.ToString();
            str += ", "                                      + this.emissive.ToString();
            str += ", " + this.shininess.ToString( "0.000" ) + "]";

            return str;
        }

        #endregion

    }

    public struct BMFMaterial {
        public Vec3  diffuse;
        public Vec3  specular;
        public Vec3  emissive;
        public float shininess;
    }

    public struct Vec2 {
        public float  x;
        public Single y;

        public Vec2(float x, float y) {
            this.x = x;
            this.y = y;
        }

        public Vec2(Single value) {
            this.x = value;
            this.y = value;
        }

        public Vec2(Vector3D v) {
            this.x = v.X;
            this.y = v.Y;
        }

        public byte[] GetBytes() {
            int    s   = sizeof(Single);
            byte[] arr = new byte[2 * s];
            Array.Copy( BitConverter.GetBytes( x ), 0, arr, s * 0, s );
            Array.Copy( BitConverter.GetBytes( y ), 0, arr, s * 1, s );

            return arr;
        }
    }

    public struct Vec3 {
        public Single x;
        public Single y;
        public Single z;

        public Vec3(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vec3(Vec2 vec, Single z) {
            this.x = vec.x;
            this.y = vec.y;
            this.z = z;
        }

        public Vec3(Single value) {
            this.x = value;
            this.y = value;
            this.z = value;
        }

        public Vec3(Vector3D v) {
            this.x = v.X;
            this.y = v.Y;
            this.z = v.Z;
        }

        public byte[] GetBytes() {
            int    s   = sizeof(Single);
            byte[] arr = new byte[3 * s];
            Array.Copy( BitConverter.GetBytes( x ), 0, arr, s * 0, s );
            Array.Copy( BitConverter.GetBytes( y ), 0, arr, s * 1, s );
            Array.Copy( BitConverter.GetBytes( z ), 0, arr, s * 2, s );

            return arr;
        }

        #region Overrides of ValueType

        /// <inheritdoc />
        public override string ToString() => "{" + this.x.ToString( "0.000" ) + ", " + this.y.ToString( "0.000" ) + ", " + this.z.ToString( "0.000" ) + "}";

        #endregion

    }

    public class Mesh {
        public List<Vec3>  positions     = new List<Vec3>();
        public List<Vec3>  normals       = new List<Vec3>();
        public List<Vec3>  tangents      = new List<Vec3>();
        public List<Vec2>  uvs           = new List<Vec2>();
        public List<Int32> indices       = new List<Int32>();
        public int         materialIndex = 0;

    };

    public struct VertexTextureAndNormal {
        Vec3 _vec3;
        Vec3 normal;
        Vec3 tangent;
        Vec2 textureCord;
    };

    public struct VertexTextureOnly {
        Vec3 _vec3;
        Vec3 normal;
        Vec2 textureCord;
    };

    public struct VertexMaterialOnly {
        Vec3 _vec3;
        Vec3 normal;
    };

    public enum VertexMode {
        TextureAndNormal,
        TextureOnly,
        MaterialOnly
    };

    #endregion

}
