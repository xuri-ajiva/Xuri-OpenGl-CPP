using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModelConverterGUI {
    public partial class MainForm : Form {
        public ModelWorker        modelWorker = new ModelWorker();
        VertexModeForm            vmode       = new VertexModeForm();
        TextBoxStreamWriter       _writer     = null;
        private        MyListView fileList;
        private        Thread     _t;
        private static TextWriter _tx = Console.Out;
        public static  FileViever FV  = new FileViever();

        public MainForm() {
            InitializeComponent();

            this.fileList = new MyListView();

            this.fileList.MouseDoubleClick += FileListOnMouseDoubleClick;
            this.fileList.KeyDown          += FileListOnKeyDown;
            this.fileList.Columns.Add( new ColumnHeader( 0 ) { Text = "File", Width            = 200 } );
            this.fileList.Columns.Add( new ColumnHeader( 0 ) { Text = "ConverterAction", Width = -2 } );
            this.spc.Panel1.Controls.Add( this.fileList );

            this.txtConsole.ScrollBars = ScrollBars.Both;
            this.fileList.View         = View.Details;

            this.fileList.Dock     = DockStyle.Fill;
            this.txtConsole.Dock   = DockStyle.Fill;
            this.button1.Dock      = DockStyle.Fill;
            this.button2.Dock      = DockStyle.Right;
            this.panel1.Dock       = DockStyle.Bottom;
            this.spc.Dock          = DockStyle.Fill;
            this.progressBar1.Dock = DockStyle.Bottom;
            this.menuStrip1.Dock   = DockStyle.None;

            this.panel1.Height        = this.button1.Height;
            this.progressBar1.Visible = false;

            this.menuStrip1.BringToFront();
            this.spc.BringToFront();

            this.modelWorker.OnActionCallback += ModelWorkerOnOnActionCallback;

            this._writer = new TextBoxStreamWriter( this.txtConsole );
            // Redirect the out Console stream
            Console.SetOut( this._writer );
        }

        private void FileListOnKeyDown(object sender, KeyEventArgs e) {
            if ( e.KeyCode == Keys.Delete && this.fileList.SelectedItems.Count > 0 ) {
                var sl = this.fileList.SelectedItems;

                foreach ( ListViewItem i in sl ) {
                    this.fileList.Items.Remove( i );
                }
            }
        }


        private void ModelWorkerOnOnActionCallback(object sender, ModelWorker.ModelWorkerEventArgs args) {
            if ( args.EventType == ModelWorker.ModelWorkerEventArgs.EventArgsType.TextureNotExists ) {
                var t = new Thread( () => {
                    using ( var of = new OpenFileDialog { FileName = args.Args, Title = "Browse For File: " + args.Args, CheckFileExists = true, InitialDirectory = args.Context } ) {
                        if ( of.ShowDialog() == DialogResult.OK ) {
                            args.Args = of.FileName;
                        }
                    }
#pragma warning disable 618
                } );
#pragma warning restore 618
                t.SetApartmentState(  ApartmentState.STA );
                t.Start();
                t.Join();
            }
        }

        private void FileListOnMouseDoubleClick(object sender, MouseEventArgs e) {
            var c = this.fileList.SelectedItems[0];

            if ( c == null ) return;

            this.vmode.UserInput( c.SubItems[0].Text, this );
            c.SubItems[1].Text = this.vmode.VMode.ToString();
        }

        private void Form1_DragDrop(object sender, DragEventArgs e) {
            var dropped = ( (string[]) e.Data.GetData( DataFormats.FileDrop ) );
            var files   = dropped.ToList();

            if ( !files.Any() )
                return;

            foreach ( var file in files ) {
                if ( !File.Exists( file ) ) continue;

                if ( !this.fileList.Items.Contains( new ListViewItem( file ) ) )
                    this.fileList.Items.Add( new ListViewItem( new[] { file, VertexMode.MaterialOnly.ToString() } ) );
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e) { e.Effect = DragDropEffects.Copy; }

        private void button1_Click(object sender, EventArgs e) {
            const int maxtext = 100000;

            foreach ( ListViewItem i in this.fileList.Items ) {
                var        fName = i.SubItems[0].Text;
                VertexMode vertexMode;

                if ( !Enum.TryParse( i.SubItems[1].Text, out vertexMode ) ) {
                    MessageBox.Show( "Internal Error" );

                    throw new ArgumentException( "Error No Known Vertex Mode", i.SubItems[1].Text );
                }

                this._t = new Thread( () => { this.modelWorker.main( fName, vertexMode ); } );
                this._t.Start();
                this.progressBar1.Visible     = true;
                this.button1.Enabled          = false;
                this.UseWaitCursor            = true;
                this.txtConsole.UseWaitCursor = true;
                this.AllowDrop                = false;
                this.menuStrip1.Enabled       = false;

                while ( this._t.IsAlive ) {
                    Application.DoEvents();
                    Thread.Sleep( 100 );

                    Invoke( new Action( () => {
                        this.txtConsole.Text           = this._writer.text;
                        this.txtConsole.SelectionStart = this.txtConsole.TextLength;
                        this.txtConsole.ScrollToCaret();
                    } ) );

                    if ( this._writer.text.Length > maxtext ) this._writer.text = this._writer.text.Substring( maxtext / 2 );
                }

                this.button1.Text             = "go";
                this.progressBar1.Visible     = false;
                this.button1.Enabled          = true;
                this.UseWaitCursor            = false;
                this.txtConsole.UseWaitCursor = false;
                this.AllowDrop                = true;
                this.menuStrip1.Enabled       = true;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            this._t?.Abort();
            Environment.Exit( 0 );
        }

        private void button2_Click(object sender, EventArgs e) {
            if ( FV != null )
                FV.Show();
            else {
                FV = new FileViever();
                FV.Show();
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            this.openFile.Multiselect = true;

            if ( this.openFile.ShowDialog( this ) == DialogResult.OK ) {
                foreach ( var file in this.openFile.FileNames ) {
                    if ( !this.fileList.Items.Contains( new ListViewItem( file ) ) )
                        this.fileList.Items.Add( new ListViewItem( new[] { file, VertexMode.MaterialOnly.ToString() } ) );
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) { Close(); }
    }

    public class TextBoxStreamWriter : TextWriter {
        public TextBox _output = null;
        public string  text    = "";

        public TextBoxStreamWriter(TextBox output) { this._output = output; }

        public override void Write(char value) {
            base.Write( value );
            this.text += value;
            //Application.DoEvents();
        }

        public override Encoding Encoding => Encoding.UTF8;
    }

    public class MyListView : ListView {
        private Container components = null;

        public MyListView() { InitializeComponent(); }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing) {
            if ( disposing ) {
                if ( this.components != null ) this.components.Dispose();
            }

            base.Dispose( disposing );
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() { components = new System.ComponentModel.Container(); }

        #endregion

        private const int WM_HSCROLL = 0x114;
        private const int WM_VSCROLL = 0x115;

        protected override void WndProc(ref Message msg) {
            // Look for the WM_VSCROLL or the WM_HSCROLL messages.
            if ( ( msg.Msg == WM_VSCROLL ) || ( msg.Msg == WM_HSCROLL ) ) {
                // Move focus to the ListView to cause ComboBox to lose focus.
                Focus();
            }

            // Pass message to default handler.
            base.WndProc( ref msg );
        }
    }
}
