using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModelConverterGUI {
    public partial class VertexModeForm : Form {
        public VertexModeForm() { InitializeComponent(); }

        public void UserInput(string file, IWin32Window owner) {
            this.textBox1.Text        = file;
            this.comboBox1.DataSource = Enum.GetValues( typeof(VertexMode) );
            ShowDialog( owner );
        }

        private void VertexModeForm_Load(object sender, EventArgs e) { }

        private void button1_Click(object sender, EventArgs e) {
            Hide();

            if ( !int.TryParse( this.comboBox1.SelectedItem.ToString(), out this.Number ) ) {
                this.VMode = (VertexMode) this.comboBox1.SelectedItem;
            }
        }

        public string     File => this.textBox1.Text;
        public VertexMode VMode  = default;
        public int        Number = 0;

        public void UserInputNumber(string info, IWin32Window owner, int start, int count) {
            this.textBox1.Text        = info;
            this.comboBox1.DataSource = Enumerable.Range( start, count ).ToList();
            ShowDialog( owner );
        }

    }
}
