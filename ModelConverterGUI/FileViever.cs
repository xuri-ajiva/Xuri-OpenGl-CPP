using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel.Design;
using System.IO;

namespace ModelConverterGUI {
    public partial class FileViever : Form {
        private ByteViewer _myByteViewer;
        private byte[]     bytes = new byte[0];

        public FileViever() {
            InitializeComponent();
            this.openFile.InitialDirectory = Path.GetDirectoryName( Application.ExecutablePath );
            this.columnHeader2.Text        = "MeshInfo [vertecies & indeces *MaterialIndex]";
            this.columnHeader3.Text        = "Texture Locations: N: normalMap D: diffuseMap";
            this.columnHeader1.Text        = "Material Info [vec3 {x,y,z}: diffuse, specular, emissive, shininess]";
            this.columnHeader3.Width       = -2;
            this.columnHeader1.Width       = 380;
            this.columnHeader2.Width       = -2;

            this._myByteViewer = new ByteViewer
                { Dock = DockStyle.Fill };
            this.viewerPanel.Controls.Add( this._myByteViewer );
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            this.listView1.Items.Clear();
            this.listView2.Items.Clear();

            if ( this.openFile.ShowDialog() == DialogResult.OK ) {
                this.bytes = File.ReadAllBytes( this.openFile.FileName );
                this._myByteViewer.SetBytes( this.bytes );
            }

            using ( var f = File.Open( this.openFile.FileName, FileMode.Open ) ) {
                using ( BinaryReader b = new BinaryReader( f ) ) {
                    VertexMode vm;
                    var        sizeFoVertex = b.ReadUInt64();
                    var        numMaterials = b.ReadUInt64();
                    var        sizeMaterial = System.Runtime.InteropServices.Marshal.SizeOf( typeof(BMFMaterial) );

                    if ( sizeFoVertex == (ulong) System.Runtime.InteropServices.Marshal.SizeOf( typeof(VertexTextureAndNormal) ) ) {
                        vm = VertexMode.TextureAndNormal;
                    }
                    else if ( sizeFoVertex == (ulong) System.Runtime.InteropServices.Marshal.SizeOf( typeof(VertexTextureOnly) ) ) {
                        vm = VertexMode.TextureOnly;
                    }
                    else if ( sizeFoVertex == (ulong) System.Runtime.InteropServices.Marshal.SizeOf( typeof(VertexMaterialOnly) ) ) {
                        vm = VertexMode.MaterialOnly;
                    }
                    else {
                        throw new Exception( "No EnumCode" );
                    }
                    this.label1.Text = "VertexMode: "     + vm;
                    this.label2.Text = "MaterialsCount: " + numMaterials;

                    for ( UInt64 i = 0; i < numMaterials; i++ ) {
                        byte[] buff = new byte[sizeMaterial];
                        b.Read( buff, 0, sizeMaterial );
                        Material material = ModelWorker.fromBytesMatertal( buff );

                        UInt64 diffuseMapNameLength = b.ReadUInt64();
                        buff = new byte[diffuseMapNameLength];
                        b.Read( buff, 0, (int) diffuseMapNameLength );
                        var dm = Encoding.UTF8.GetString( buff );

                        UInt64 normalMapNameLength = b.ReadUInt64();
                        buff = new byte[normalMapNameLength];
                        b.Read( buff, 0, (int) normalMapNameLength );
                        var nm = Encoding.UTF8.GetString( buff );

                        this.listView1.Items.Add( new ListViewItem( new[] { material.ToString(), "D: " + dm + "   N: " + nm } ) );
                    }

                    var numMeshes = b.ReadUInt64();

                    for ( UInt64 i = 0; i < numMeshes; i++ ) {
                        UInt64 materialIndex = b.ReadUInt64();
                        UInt64 numVertices   = b.ReadUInt64();
                        UInt64 numIndices    = b.ReadUInt64();

                        for ( UInt64 k = 0; k < numVertices; k++ ) {
                            var buff = new byte[sizeFoVertex];
                            b.Read( buff, 0, (int) sizeFoVertex );
                            //
                        }

                        for ( UInt64 g = 0; g < numIndices; g++ ) {
                            UInt32 index = b.ReadUInt32();
                            //indices.push_back(index);
                        }

                        this.listView2.Items.Add( "[" + numVertices + " & " + numIndices + " *" + materialIndex + "]" );
                    }

                    this.label3.Text = "MeshesCounr: "    + numMeshes;
                    
                    FileViever_Validated( sender, null );
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) { Close(); }

        private void FileViever_Load(object sender, EventArgs e) { }

        private void FileViever_Validated(object sender, EventArgs e) {
            this.columnHeader3.Width = -2;
            this.columnHeader1.Width = 380;
            this.columnHeader2.Width = -2;
        }

        private void splitter1_SplitterMoved(object sender, SplitterEventArgs e) { FileViever_Validated( sender, null ); }
    }
}
