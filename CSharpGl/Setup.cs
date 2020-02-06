using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace CSharpGl {
    class Setup {
        public float time         = 0.0F;
        public Int64 frames       = 0;


        public static GameWindow window;

    #if DEBUG
        static void Callback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userparam) {
            //if(serverity == GL_DEBUG_SEVERITY_HIGH)

            Console.WriteLine( "[Open Info: {severity: " + severity + ", source: " + source + ", type: " + type + ", id: " + id + "}]:\n    " + message );
        }
    #endif

        public static int Init() {
            Console.WriteLine( "Starting App..." );

            GameWindow g = new GameWindow( 1000, 800, GraphicsMode.Default, "Xuri´s OpenGl C#", GameWindowFlags.Default, DisplayDevice.Default );

            var glContext = g.Context;

            var err = GL.GetError();

            if ( err != ErrorCode.NoError ) {
                Console.WriteLine( err );
                Console.Read();
                return -1;
            }

        #if DEBUG
            GL.Enable( EnableCap.DebugOutput );
            GL.Enable( EnableCap.DebugOutputSynchronous );
            GL.DebugMessageCallback( Callback, IntPtr.Zero);
        #endif


            window = g;
            return 0;
        }
    }
}
