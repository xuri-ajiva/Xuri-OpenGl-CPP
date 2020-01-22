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

        private FileStream   fs = default;
        private BinaryReader br = default;
        private BinaryWriter bw = default;
        VertexModeForm       UI = new VertexModeForm();

        ulong sizeOfVertex;
        ulong numMaterials;
        ulong numMeshes;
        int   sizeMaterial;

        VertexMode vm;

        public FileViever() {
            InitializeComponent();
            this.openFile.InitialDirectory = Path.GetDirectoryName( Application.ExecutablePath );
            this.columnHeader6.Text        = "Index";
            this.columnHeader2.Text        = "numVertecies";
            this.columnHeader4.Text        = "numIndeces";
            this.columnHeader5.Text        = "MaterialIndex";
            this.columnHeader3.Text        = "Texture Locations: N: normalMap D: diffuseMap";
            this.columnHeader1.Text        = "Material Info [vec3 {x,y,z}: diffuse, specular, emissive, shininess] color";
            this.columnHeader3.Width       = -2;
            this.columnHeader1.Width       = 380;
            this.columnHeader2.Width       = 100;
            this.columnHeader4.Width       = 100;
            this.columnHeader5.Width       = -2;
            this.columnHeader6.Width       = 20;

            this._myByteViewer = new ByteViewer
                { Dock = DockStyle.Fill };
            this.viewerPanel.Controls.Add( this._myByteViewer );
        }

        bool readFirstInfo() {
            if ( this.fs == null ) return false;

            this.br = new BinaryReader( this.fs );
            this.bw = new BinaryWriter( this.fs );

            sizeOfVertex = this.br.ReadUInt64();
            numMaterials = this.br.ReadUInt64();
            sizeMaterial = System.Runtime.InteropServices.Marshal.SizeOf( typeof(BMFMaterial) );

            if ( sizeOfVertex == (ulong) System.Runtime.InteropServices.Marshal.SizeOf( typeof(VertexTextureAndNormal) ) ) {
                vm = VertexMode.TextureAndNormal;
            }
            else if ( sizeOfVertex == (ulong) System.Runtime.InteropServices.Marshal.SizeOf( typeof(VertexTextureOnly) ) ) {
                vm = VertexMode.TextureOnly;
            }
            else if ( sizeOfVertex == (ulong) System.Runtime.InteropServices.Marshal.SizeOf( typeof(VertexMaterialOnly) ) ) {
                vm = VertexMode.MaterialOnly;
            }
            else {
                return false;
                throw new Exception( "No EnumCode" );
            }

            this.label1.Text = "VertexMode: "     + vm;
            this.label2.Text = "MaterialsCount: " + numMaterials;

            return true;
        }


        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            this.listView1.Items.Clear();
            this.listView2.Items.Clear();

            if ( this.openFile.ShowDialog() == DialogResult.OK ) {
                LoadFile( this.openFile.FileName );
            }
        }

        void LoadFile(string FileName) {
            this.fs?.Close();
            this.bytes = File.ReadAllBytes( FileName );
            this._myByteViewer.SetBytes( this.bytes );

            this.fs = File.Open( FileName, FileMode.Open );
            if(!readFirstInfo()) return;

            for ( UInt64 i = 0; i < numMaterials; i++ ) {
                byte[] buff = new byte[sizeMaterial];
                this.br.Read( buff, 0, sizeMaterial );
                Material material = ModelWorker.fromBytesMatertal( buff );

                UInt64 diffuseMapNameLength = this.br.ReadUInt64();
                buff = new byte[diffuseMapNameLength];
                this.br.Read( buff, 0, (int) diffuseMapNameLength );
                string dm = Encoding.UTF8.GetString( buff );

                UInt64 normalMapNameLength = this.br.ReadUInt64();
                buff = new byte[normalMapNameLength];
                this.br.Read( buff, 0, (int) normalMapNameLength );
                string nm = Encoding.UTF8.GetString( buff );

                this.listView1.Items.Add( new ListViewItem( new[] { material.ToString(), "D: " + dm + "   N: " + nm } ) );
            }

            numMeshes = this.br.ReadUInt64();

            for ( UInt64 i = 0; i < numMeshes; i++ ) {
                UInt64 materialIndex = this.br.ReadUInt64();
                UInt64 numVertices   = this.br.ReadUInt64();
                UInt64 numIndices    = this.br.ReadUInt64();

                byte[] buff = new byte[numVertices * sizeOfVertex];
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

                this.listView2.Items.Add( new ListViewItem( new[] { numVertices.ToString(), numIndices.ToString(), materialIndex.ToString() } ) );
            }

            this.label3.Text = "MeshesCounr: " + numMeshes;

            FileViever_Validated( null, null );
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) { Close(); }

        private void FileViever_Validated(object sender, EventArgs e) {
            this.columnHeader3.Width = -2;
            this.columnHeader5.Width = -2;
        }

        private void splitter1_SplitterMoved(object sender, SplitterEventArgs e) { FileViever_Validated( sender, null ); }

        List<ChangedItem> changes = new List<ChangedItem>();

        struct ChangedItem {
            public UInt64 MeshIndex;
            public UInt64 MaterialIndex;

            public ChangedItem(UInt64 meshIndex, UInt64 materialIndex) {
                this.MeshIndex     = meshIndex;
                this.MaterialIndex = materialIndex;
            }
        }

        bool contains(UInt64 MeshIndex, out UInt64 materialIndex) {
            for ( int i = 0; i < this.changes.Count; i++ ) {
                if ( this.changes[i].MeshIndex == MeshIndex ) {
                    materialIndex = this.changes[i].MaterialIndex;

                    return true;
                }
            }

            materialIndex = 0;

            return false;
        }

        private void listView2_MouseDoubleClick(object sender, MouseEventArgs e) {
            this.UI.UserInputNumber( "Set Material index", this, 0, this.listView1.Items.Count );
            this.listView2.SelectedItems[0].SubItems[2].Text = this.UI.Number.ToString();

            this.changes.Add( new ChangedItem( (UInt64) this.listView2.Items.IndexOf( this.listView2.SelectedItems[0] ), (UInt64) this.UI.Number ) );
        }

        private void button1_Click(object sender, EventArgs e) {
            this.fs.Position = 0;
            listView2.Items.Clear();

            if(!readFirstInfo()) return;

            for ( UInt64 i = 0; i < numMaterials; i++ ) {
                this.fs.Position += this.sizeMaterial;

                UInt64 diffuseMapNameLength = this.br.ReadUInt64();
                this.fs.Position += (long) diffuseMapNameLength;

                UInt64 normalMapNameLength = this.br.ReadUInt64();
                this.fs.Position += (long) normalMapNameLength;
            }

            numMeshes = this.br.ReadUInt64();

            for ( UInt64 i = 0; i < numMeshes; i++ ) {
                if ( contains( i, out var MatI ) ) {
                    this.bw.Write( MatI );
                    this.fs.Position -= sizeof(UInt64);
                }

                UInt64 materialIndex = this.br.ReadUInt64();
                UInt64 numVertices   = this.br.ReadUInt64();
                UInt64 numIndices    = this.br.ReadUInt64();

                var buff = new byte[numVertices * sizeOfVertex];
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

                this.listView2.Items.Add( new ListViewItem( new[] { numVertices.ToString(), numIndices.ToString(), materialIndex.ToString() } ) );
            }
            this.changes.Clear();
        }

        private void button2_Click(object sender, EventArgs e) {
            this.fs?.Close();
            this.br?.Close();
            this.bw?.Close();
            this.fs?.Dispose();
            this.br?.Dispose();
            this.bw?.Dispose();
            this.fs = null;
            this.br = null;
            this.bw = null;

            var buff = new byte[0];
            this._myByteViewer.SetBytes( buff );
            this.listView1.Items.Clear();
            this.listView2.Items.Clear();
            this.label1.Text = "Open File";
            this.label2.Text = "Open File";
            this.label3.Text = "Open File";
        }

        private void FileViever_DragDrop(object sender, DragEventArgs e) {
            var dropped = ( (string[]) e.Data.GetData( DataFormats.FileDrop ) );
            var files   = dropped.ToList();

            if ( !files.Any() )
                return;

            if ( !File.Exists( files[0] ) ) return;

            LoadFile( files[0] );
        }

        private void FileViever_DragEnter(object sender, DragEventArgs e) {
            var str = e.Data.GetData( DataFormats.FileDrop ) as string[];

            if ( !str.Any() ) return;

            e.Effect = str.Length == 1 ? DragDropEffects.Copy : DragDropEffects.None;
        }

    }
}
