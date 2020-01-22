using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModelConverterGUI {
    static class Program {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );

            if ( args.Length >= 1 ) {
                foreach ( var s in args ) {
                    if ( File.Exists( s ) ) {
                        var f = new VertexModeForm();
                        f.UserInput( s, null );
                        new ModelWorker().main( s, f.VMode );
                    }
                    else {
                        Console.WriteLine( "Expect existing file got: " + s );
                    }
                }
            }

            Application.Run( new MainForm() );
        }
    }
}
