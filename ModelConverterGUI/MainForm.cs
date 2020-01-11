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
        public ModelWorker  modelWorker = new ModelWorker();
        VertexModeForm      vmode       = new VertexModeForm();
        TextBoxStreamWriter _writer     = null;
        private MyListView  fileList;
        private Thread      t;

        public MainForm() {
            InitializeComponent();

            this.fileList = new MyListView();
            this.fileList.Columns.Add( new ColumnHeader( 0 ) { Text = "File", Width = -2 } );
            this.fileList.View         = View.Details;
            this.fileList.Dock         = DockStyle.Fill;
            this.txtConsole.Dock       = DockStyle.Fill;
            this.txtConsole.ScrollBars = ScrollBars.Both;
            this.spc.Panel1.Controls.Add( this.fileList );
            this.button1.Dock         = DockStyle.Bottom;
            this.spc.Dock             = DockStyle.Fill;
            this.progressBar1.Dock    = DockStyle.Bottom;
            this.progressBar1.Visible = false;
            this.spc.BringToFront();

            this._writer = new TextBoxStreamWriter( this.txtConsole );
            // Redirect the out Console stream
            Console.SetOut( this._writer );
        }

        private void Form1_DragDrop(object sender, DragEventArgs e) {
            var dropped = ( (string[]) e.Data.GetData( DataFormats.FileDrop ) );
            var files   = dropped.ToList();

            if ( !files.Any() )
                return;

            foreach ( string file in files ) {
                if ( !File.Exists( file ) ) continue;

                if ( !this.fileList.Items.Contains( new ListViewItem( file ) ) )
                    this.fileList.Items.Add( file );
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e) { e.Effect = DragDropEffects.Copy; }

        private void button1_Click(object sender, EventArgs e) {
            const int maxtext = 100000;
            foreach ( ListViewItem i in this.fileList.Items ) {
                this.vmode.UserInput( i.Text, this );

                this.t = new Thread( () => { this.modelWorker.main( this.vmode.File, this.vmode.VMode ); } );
                this.t.Start();
                this.progressBar1.Visible     = true;
                this.button1.Enabled          = false;
                this.UseWaitCursor            = true;
                this.txtConsole.UseWaitCursor = true;

                while ( this.t.IsAlive ) {
                    Application.DoEvents();
                    Thread.Sleep( 100 );

                    Invoke( new Action( () => {
                        this.txtConsole.Text           = this._writer.text;
                        this.txtConsole.SelectionStart = this.txtConsole.TextLength;
                        this.txtConsole.ScrollToCaret();
                    } ) );

                    if ( this._writer.text.Length > maxtext ) this._writer.text = this._writer.text.Substring(maxtext/2  );
                }

                this.button1.Text             = "go";
                this.progressBar1.Visible     = false;
                this.button1.Enabled          = true;
                this.UseWaitCursor            = false;
                this.txtConsole.UseWaitCursor = false;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            this.t?.Abort();
            Environment.Exit( 0 );
        }
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

        public override Encoding Encoding { get { return Encoding.UTF8; } }
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
