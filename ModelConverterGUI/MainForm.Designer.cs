namespace ModelConverterGUI
{
    partial class MainForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.spc = new System.Windows.Forms.SplitContainer();
            this.txtConsole = new System.Windows.Forms.TextBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            ((System.ComponentModel.ISupportInitialize)(this.spc)).BeginInit();
            this.spc.Panel2.SuspendLayout();
            this.spc.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(362, 286);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "go";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // spc
            // 
            this.spc.Location = new System.Drawing.Point(12, 12);
            this.spc.Name = "spc";
            this.spc.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // spc.Panel2
            // 
            this.spc.Panel2.Controls.Add(this.txtConsole);
            this.spc.Size = new System.Drawing.Size(455, 242);
            this.spc.SplitterDistance = 149;
            this.spc.TabIndex = 3;
            // 
            // txtConsole
            // 
            this.txtConsole.AcceptsReturn = true;
            this.txtConsole.AcceptsTab = true;
            this.txtConsole.Location = new System.Drawing.Point(30, 18);
            this.txtConsole.Multiline = true;
            this.txtConsole.Name = "txtConsole";
            this.txtConsole.ReadOnly = true;
            this.txtConsole.Size = new System.Drawing.Size(372, 45);
            this.txtConsole.TabIndex = 0;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(134, 260);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(100, 17);
            this.progressBar1.Step = 100;
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar1.TabIndex = 4;
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(474, 317);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.spc);
            this.Controls.Add(this.button1);
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Form1_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Form1_DragEnter);
            this.spc.Panel2.ResumeLayout(false);
            this.spc.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.spc)).EndInit();
            this.spc.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.SplitContainer spc;
        private System.Windows.Forms.TextBox txtConsole;
        private System.Windows.Forms.ProgressBar progressBar1;
    }
}

