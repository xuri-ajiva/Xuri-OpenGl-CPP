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
            this.textBox1.Text = file;
            this.ShowDialog(owner);
        }

        private void VertexModeForm_Load(object sender, EventArgs e) { this.comboBox1.DataSource = Enum.GetValues(typeof(VertexMode));; }

        private void button1_Click(object sender, EventArgs e) {
            this.Hide();
            VMode = (VertexMode) comboBox1.SelectedItem;
        }

        public string File => this.textBox1.Text;
        public VertexMode VMode = default;
    }
}
