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
                    Position position = new Position( mesh.Vertices[i] );
                    m.positions.Add( position );
                }
                else {
                    Console.WriteLine( "[" + i + "]: " + "No Vertices" );
                }

                if ( mesh.HasNormals ) {
                    Position normal = new Position( mesh.Normals[i] );
                    m.normals.Add( normal );
                }
                else {
                    Console.WriteLine( "[" + i + "]: " + "No Normals" );
                }

                if ( mode == VertexMode.TextureAndNormal )
                    if ( mesh.HasTangentBasis ) {
                        Position tangent = new Position( mesh.Tangents[i] );
                        m.tangents.Add( tangent );
                    }
                    else {
                        Console.WriteLine( "[" + i + "]: " + "No Tangents" );
                    }

                if ( mode == VertexMode.TextureAndNormal || mode == VertexMode.TextureOnly )
                    if ( mesh.HasTextureCoords( 0 ) ) {
                        Position2D uv = new Position2D( mesh.TextureCoordinateChannels[0][i] );
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

        void ProcessMaterials(Scene scene) {
            for ( int i = 0; i < scene.MaterialCount; i++ ) {
                Material        mat      = new Material();
                Assimp.Material material = scene.Materials[i];

                var diffuse = material.ColorDiffuse;
                mat.diffuse = new Position( diffuse.R, diffuse.G, diffuse.B );

                var specular = material.ColorSpecular;
                mat.specular = new Position( specular.R, specular.G, specular.B );

                var emissive = material.ColorEmissive;
                mat.emissive = new Position( emissive.R, emissive.G, emissive.B );

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
                    mat.diffuseMapName = diffuseMapName.FilePath;
                }

                if ( numNormalMaps > 0 ) {
                    material.GetMaterialTexture( TextureType.Normals, 0, out var normalMapName );
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

            ProcessMaterials( scene );
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
                    UInt64       diffuesMapNameLength = UInt64.MinValue;
                    UInt64       normalMapNameLength  = UInt64.MinValue;

                    // Diffuse map
                    if ( mode == VertexMode.TextureOnly || mode == VertexMode.TextureAndNormal ) {
                        diffuesMapNameLength = (UInt64) ( string.IsNullOrEmpty( material.diffuseMapName ) ? 0 : material.diffuseMapName.Length + pathPrefix.Length );
                    }

                    br.Write( diffuesMapNameLength );

                    if ( diffuesMapNameLength > 0 && ( mode == VertexMode.TextureOnly || mode == VertexMode.TextureAndNormal ) ) {
                        br.Write( Encoding.UTF8.GetBytes( pathPrefix + material.diffuseMapName ) );
                        Console.WriteLine( pathPrefix + material.diffuseMapName );
                    }

                    // Normal map
                    if ( mode == VertexMode.TextureAndNormal ) {
                        normalMapNameLength = (UInt64) ( string.IsNullOrEmpty( material.normalMapName ) ? 0 : material.normalMapName.Length + pathPrefix.Length );
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
    }


    #region Structe

    public struct Material {
        public Position diffuse;
        public Position specular;
        public Position emissive;
        public float    shininess;
        public string   diffuseMapName;
        public string   normalMapName;

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
            str += "[" +this.diffuse.ToString();
            str += ", "+this.specular.ToString();
            str += ", "+this.emissive.ToString();
            str += ", "+this.shininess.ToString("0.000") + "]";

            return str;
        }

        #endregion

    }

    public struct BMFMaterial {
        public Position diffuse;
        public Position specular;
        public Position emissive;
        public float    shininess;
    }

    public struct Position2D {
        public float  x;
        public Single y;

        public Position2D(float x, float y) {
            this.x = x;
            this.y = y;
        }

        public Position2D(Single value) {
            this.x = value;
            this.y = value;
        }

        public Position2D(Vector3D v) {
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

    public struct Position {
        public Single x;
        public Single y;
        public Single z;

        public Position(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Position(Position2D vec, Single z) {
            this.x = vec.x;
            this.y = vec.y;
            this.z = z;
        }

        public Position(Single value) {
            this.x = value;
            this.y = value;
            this.z = value;
        }

        public Position(Vector3D v) {
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
        public override string ToString() => "{"+ this.x.ToString("0.000") + ", "+ this.y.ToString("0.000") +", " + this.z.ToString("0.000") +"}";

        #endregion

    }

    public class Mesh {
        public List<Position>   positions     = new List<Position>();
        public List<Position>   normals       = new List<Position>();
        public List<Position>   tangents      = new List<Position>();
        public List<Position2D> uvs           = new List<Position2D>();
        public List<Int32>      indices       = new List<Int32>();
        public int              materialIndex = 0;

    };

    public struct VertexTextureAndNormal {
        Position   position;
        Position   normal;
        Position   tangent;
        Position2D textureCord;
    };

    public struct VertexTextureOnly {
        Position   position;
        Position   normal;
        Position2D textureCord;
    };

    public struct VertexMaterialOnly {
        Position position;
        Position normal;
    };

    public enum VertexMode {
        TextureAndNormal,
        TextureOnly,
        MaterialOnly
    };

    #endregion

}
