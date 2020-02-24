using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSharpGl.Cameras;
using GlmNet;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using KeyPressEventArgs = OpenTK.KeyPressEventArgs;
using MatrixMode = OpenTK.Graphics.OpenGL.MatrixMode;

namespace CSharpGl {
    public static class exth {
        public const ConsoleColor MESSAGE_COLOR = ConsoleColor.DarkYellow;

        public const ConsoleColor PLUS_COLOR  = ConsoleColor.Green;
        public const ConsoleColor MINUS_COLOR = ConsoleColor.Red;

        public static vec4    V(this Vector4 v) { return new vec4( v.X, v.Y, v.Z, v.W ); }
        public static vec3    V(this Vector3 v) { return new vec3( v.X, v.Y, v.Z ); }
        public static Vector3 V(this vec3    v) { return new Vector3( v.x, v.y, v.z ); }

        public static Matrix4 M(this mat4 m) {
            var array = m.to_array();

            return new Matrix4( array[0], array[1], array[2], array[3], array[4], array[5], array[6], array[7], array[8], array[9], array[10], array[11], array[12], array[13], array[14], array[15] );
        }

        public static mat4 M(this Matrix4 m) { return new mat4( m.Column0.V(), m.Column1.V(), m.Column2.V(), m.Column3.V() ); }

        public static void PrintStatus(char SIndex, int value, string message, int c, ConsoleColor messageColor) {
            Console.ForegroundColor = (ConsoleColor) c;
            Console.Write( SIndex + "[" + value + "]: " );
            Console.ForegroundColor = messageColor;
            Console.WriteLine( message );
        }
    }
    class Program {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );
            var p = new Program( false );
            p.run();
        }
    #if DEBUG
        void GLGetError(string file, int line, string call) {
            ErrorCode error = ErrorCode.NoError;

            do {
                error = GL.GetError();
                if ( error == ErrorCode.NoError ) return;

                Console.WriteLine( "[Open info: {file: " +
                                   file                  +
                                   ", line: "            +
                                   line                  +
                                   ", call: "            +
                                   call                  +
                                   ", error: "           +
                                   error.ToString()      +
                                   "}]: "
                    //+glewGetErrorString( error )
                );
            } while ( error != ErrorCode.NoError );
        }

    #endif

        string ShaderPos      = "shaders/";
        string ModelsPos      = "models/";
        string vertexShader   = "basic-vertex-shader.glsl.vert";
        string fragmentShader = "basic-fragment-shader.glsl.frag";


        Vector3            sunDirection;
        Vector3            sunColor;
        int                directionLocation;
        Vector3            spotColor;
        Shader             shader;
        Vector4            pointLightPosition;
        int                pointLightLocation;
        List<Model>        models = new List<Model>();
        List<Matrix4>      mats   = new List<Matrix4>();
        private GameWindow window;


        private Vector3[] vertices;

        public Program(bool fírst) {
            InternalInit();

            unsafe {
                vertices = new[] {
                    new Vector3( -0.5f, -0.5f, 0.0f ),
                    new Vector3( 0.0f,  0.5f,  0.0f ),
                    new Vector3( 0.5f,  -0.5f, 0.0f )
                };

                int vertexBuffer = GL.GenBuffer();
                GL.BindBuffer( BufferTarget.ArrayBuffer, vertexBuffer );
                GL.BufferData( BufferTarget.ArrayBuffer, vertices.Length * sizeof(Vector3), vertices, BufferUsageHint.StaticDraw );

                GL.EnableVertexAttribArray( 0 );
                GL.VertexAttribPointer( 0, 3, VertexAttribPointerType.Float, false, sizeof(Vector3), Marshal.OffsetOf( typeof(Vector3), "X" ) );
            }
        }

        void InternalInit() {
            //Application.Run(new Form1());
            var g = new OpenTK.GameWindow( 1000, 1000, GraphicsMode.Default, "Xuri´s OpenGl C#" );
            OpenTK.Graphics.OpenGL. GL.MatrixMode( MatrixMode.Modelview );
            OpenTK.Graphics.OpenGL. GL.LoadIdentity();

            GL.Enable( EnableCap.Texture2D );
            GL.BindTexture( TextureTarget.Texture2D, 0 );
            GL.Enable( EnableCap.Blend );
            GL.Enable( EnableCap.DepthTest );
            GL.ClearColor( Color.CadetBlue );

            g.Load += (sender, e) => {
                g.VSync = OpenTK.VSyncMode.On;
            };

            g.Resize += (sender, e) => {
                OpenTK.Graphics.OpenGL. GL.MatrixMode( MatrixMode.Projection );
                OpenTK.Graphics.OpenGL. GL.LoadIdentity();
                OpenTK.Graphics.OpenGL.  GL.Ortho( 0, g.ClientSize.Width, g.ClientSize.Height, 0, -1.0, 1.0 );
                OpenTK.Graphics.OpenGL. GL.Viewport( 0, 0, g.ClientSize.Width, g.ClientSize.Height );
                OpenTK.Graphics.OpenGL.  GL.MatrixMode( MatrixMode.Modelview );
            };




            this.window   =  g;
            g.UpdateFrame += GOnUpdateFrame;
            g.RenderFrame += GOnRenderFrame;
            g.KeyPress    += GOnKeyPress;
            g.KeyDown     += GOnKeyDown;
            g.KeyUp       += GOnKeyUp;
            g.MouseMove   += GOnMouseMove;
            g.MouseDown   += GOnMouseDown;

            int init_err = Setup.Init();

            if ( init_err != 0 ) {
                Console.WriteLine( "init error: " + init_err );
                Console.Read();
                Environment.Exit( init_err );
            }

            Console.WriteLine( "OpenGl version: " + GL.GetString( StringName.Version ) );



        }

        private void GOnUpdateFrame(object sender, FrameEventArgs e) { Console.WriteLine( e.Time ); }

        public Program() {
            InternalInit();

            Console.WriteLine( "Shaders: " + ShaderPos + "\n	[" + vertexShader + ", " + fragmentShader + "]" );

            shader = new Shader( ( this.ShaderPos + this.vertexShader ), ( this.ShaderPos + this.fragmentShader ) );
            shader.bind();
            PrintStatus( 'S', shader.GetShaderID(), "-> Shader", 9, 1 );

            directionLocation = GL.GetUniformLocation( shader.GetShaderID(), "u_directional_light.direction" );
            sunColor          = new Vector3( .8f );
            sunDirection      = new Vector3( 0f, -1f, 0f );
            var z    = new Vector3( 0.0000001f );
            int zero = 0;
            GL.Uniform3( GL.GetUniformLocation( shader.GetShaderID(), "u_directional_light.diffuse" ),  ref sunColor );
            GL.Uniform3( GL.GetUniformLocation( shader.GetShaderID(), "u_directional_light.specular" ), ref z );
            sunColor *= 0.2F;
            GL.Uniform3( GL.GetUniformLocation( shader.GetShaderID(), "u_directional_light.ambient" ), ref sunColor );
            //GL.Uniform1iv(GL.GetUniformLocation(shader.GetShaderID(), "u_use_sun"),1,(int*)&zero));

            Vector3 pointColor = new Vector3( 0, 0, 30 );
            //pointColor *= 0;
            GL.Uniform3( GL.GetUniformLocation( shader.GetShaderID(), "u_point_light.diffuse" ),  ref pointColor );
            GL.Uniform3( GL.GetUniformLocation( shader.GetShaderID(), "u_point_light.specular" ), ref pointColor );
            pointColor *= 0.2F;
            GL.Uniform3( GL.GetUniformLocation( shader.GetShaderID(), "u_point_light.ambient" ), ref pointColor );
            GL.Uniform1( GL.GetUniformLocation( shader.GetShaderID(), "u_point_light.linear" ),    0.027f );
            GL.Uniform1( GL.GetUniformLocation( shader.GetShaderID(), "u_point_light.quadratic" ), 0.0028f );
            pointLightPosition = new Vector4( 1f, 1f, 10f, 1f );
            pointLightLocation = GL.GetUniformLocation( this.shader.GetShaderID(), "u_point_light.position" );

            spotColor = new Vector3( 2 );
            //spotColor *= 0;
            GL.Uniform3( GL.GetUniformLocation( shader.GetShaderID(), "u_spot_light.diffuse" ),  ref spotColor );
            GL.Uniform3( GL.GetUniformLocation( shader.GetShaderID(), "u_spot_light.specular" ), ref spotColor );
            spotColor *= 0.1F;
            GL.Uniform3( GL.GetUniformLocation( shader.GetShaderID(), "u_spot_light.ambient" ), ref spotColor );
            Vector3 spotLightPosition = new Vector3( 0.0F );
            GL.Uniform3( GL.GetUniformLocation( shader.GetShaderID(), "u_spot_light.position" ), ref spotLightPosition );
            spotLightPosition.Z = 1.0f;
            GL.Uniform3( GL.GetUniformLocation( shader.GetShaderID(), "u_spot_light.direction" ), ref spotLightPosition );
            GL.Uniform1( GL.GetUniformLocation( shader.GetShaderID(), "u_spot_light.innerCone" ), 0.99f );
            GL.Uniform1( GL.GetUniformLocation( shader.GetShaderID(), "u_spot_light.outerCone" ), 0.98f );

            string path = ( ModelsPos );
            string ext  = ( ".bmf" );
            int    idx  = 0;

            foreach ( var p in new DirectoryInfo( path ).GetFiles() ) {
                if ( p.Extension == ext ) {
                    Console.WriteLine( "Loading: " + p + '\n' );
                    var   t     = p.FullName;
                    Model model = new Model( t, shader );
                    model.Init();
                    models.Add( model );
                    var Matrix4X = new mat4( 1.0F );
                    Matrix4X = glm.translate( Matrix4X, new vec3( 100 * ( idx++ ), 0, 0 ) );
                    Matrix4X = glm.scale( Matrix4X, new vec3( .2f ) );
                    mats.Add( Matrix4X.M() );
                }
            }

            camModelRender = glm.scale( new mat4( 1.0F ), new vec3( .2f ) ).M();

            //model_rotate      = scale(model_rotate, Vector3(.1F));

            camera = new FPSCamera( 90.0F, 1000, 800 );
            camera.Translate( new Vector3( 0, 0, 5.0f ) );
            camera.update();

            modelViewProj = camera.GetViewProj() * camModelRender;

            modelViewProjMatrixLocation = GL.GetUniformLocation( this.shader.GetShaderID(), "u_modelViewProj" );

            modelViewLocation    = GL.GetUniformLocation( this.shader.GetShaderID(), "u_modelView" );
            invModelViewLocation = GL.GetUniformLocation( this.shader.GetShaderID(), "u_invModelView" );

            //GL.PolygonMode (GL_FRONT_AND_BACK,GL_LINE);

            float camaraSpeed = 60.0F;

            GL.Enable( EnableCap.DepthTest );
        }

        private int run() {
            this.window.Run( 120, 60 );

            return 0;
        }

        private void GOnMouseDown(object sender, MouseButtonEventArgs e) { this.window.CursorGrabbed = true; }

        private void GOnMouseMove(object sender, MouseMoveEventArgs e) {
            if ( this.window.CursorGrabbed ) camera.onMouseMoved( e.XDelta, e.YDelta );
        }

        private void GOnKeyUp(object sender, KeyboardKeyEventArgs e) { GOnKey( sender, e, false ); }

        private void GOnKeyDown(object sender, KeyboardKeyEventArgs e) { GOnKey( sender, e, true ); }

        private void GOnKey(object sender, KeyboardKeyEventArgs e, bool state) {
            switch (e.Key) {
                case Key.Escape:
                    Environment.Exit( 0 );
                    break;

                case Key.W:
                    b_W = state;
                    break;

                case Key.A:
                    b_A = state;
                    break;

                case Key.S:
                    b_S = state;
                    break;

                case Key.D:
                    b_D = state;
                    break;

                case Key.Q:
                    b_Q = state;
                    break;

                case Key.E:
                    b_E = state;
                    break;

                case Key.F1:
                    if ( state ) camera.SetFreeCam( !camera.GetFreeCam() );
                    break;

                case Key.F2:
                    if ( state ) this.window.CursorGrabbed = false;
                    break;

                case Key.F:
                    if ( state ) b_F = !b_F;
                    break;

                default:
                    break;
            }
        }

        private void GOnKeyPress(object sender, KeyPressEventArgs e) {
            /*
               //if (event.key.keysym.sym == SDLK_p && event.key.keysym.mod & KMOD_LALT) {	//	if (orthoCam) {	//		projection = perspective;	//	}	//	else {	//		projection = ortho;	//	}	//	//	orthoCam = !orthoCam;	//}	
               const var state = event.type == SDL_KEYDOWN;

               switch (event.

               key.keysym.sym) {
                   case SDLK_ESCAPE:
                   return 0;

                   case SDLK_w:
                   b_W = state;
                   break;

                   case SDLK_a:
                   b_A = state;
                   break;

                   case SDLK_s:
                   b_S = state;
                   break;

                   case SDLK_d:
                   b_D = state;
                   break;

                   case SDLK_q:
                   b_Q = state;
                   break;

                   case SDLK_e:
                   b_E = state;
                   break;

                   case SDLK_F1:
                   if ( state ) camera.SetFreeCam( !camera.GetFreeCam() );
                   break;

                   case SDLK_F2:
                   if ( state ) SDL_SetRelativeMouseMode( SDL_FALSE );
                   break;

                   case SDLK_f:
                   if ( state ) b_F = !b_F;
                   break;

                   default: ;
               }
               if (event.key.keysym.mod & KMOD_LSHIFT) {
                   camaraSpeed = 140.0;
               } else if (event.key.keysym.mod & KMOD_LCTRL) {
                   camaraSpeed = 2.0;
               } else {
                   camaraSpeed = 60.0;
               }

               //if (event.key.keysym.sym == SDLK_p && event.key.keysym.mod & KMOD_LALT) { }
           } else if (event.type == SDL_MOUSEMOTION) {
               if ( SDL_GetRelativeMouseMode() ) camera.onMouseMoved(  event.
               motion.xrel, event.motion.yrel);
           } else if (event.type == SDL_MOUSEBUTTONDOWN) {
               if (event.button.button == SDL_BUTTON_LEFT) {
                   SDL_SetRelativeMouseMode( SDL_TRUE );
               }
           } else if (event.type == SDL_WINDOWEVENT) {
               int w, h;
               SDL_GetWindowSize( window, &w, &h );
               GL.Viewport( 0, 0, w, h );
           }
           //else if (event.type == SDL_WINDOWEVENT_FOCUS_LOST) {
           //	SDL_SetRelativeMouseMode(SDL_FALSE);
           //}  */
        }

        int           invModelViewLocation;
        int           modelViewLocation;
        int           modelViewProjMatrixLocation;
        Matrix4       camModelRender;
        FPSCamera     camera;
        Matrix4       modelViewProj;
        bool          b_W = false;
        bool          b_S = false;
        bool          b_A = false;
        bool          b_D = false;
        bool          b_Q = false;
        bool          b_E = false;
        bool          b_F = false;
        private float camaraSpeed;

        void keys() { /*
                                if (event.type == SDL_KEYDOWN || event.type == SDL_KEYUP) {
                        //if (event.key.keysym.sym == SDLK_p && event.key.keysym.mod & KMOD_LALT) {	//	if (orthoCam) {	//		projection = perspective;	//	}	//	else {	//		projection = ortho;	//	}	//	//	orthoCam = !orthoCam;	//}	
                        const var state = event.type == SDL_KEYDOWN;

                        switch (event.

                        key.keysym.sym) {
                            case SDLK_ESCAPE:
                            return 0;

                            case SDLK_w:
                            b_W = state;
                            break;

                            case SDLK_a:
                            b_A = state;
                            break;

                            case SDLK_s:
                            b_S = state;
                            break;

                            case SDLK_d:
                            b_D = state;
                            break;

                            case SDLK_q:
                            b_Q = state;
                            break;

                            case SDLK_e:
                            b_E = state;
                            break;

                            case SDLK_F1:
                            if ( state ) camera.SetFreeCam( !camera.GetFreeCam() );
                            break;

                            case SDLK_F2:
                            if ( state ) SDL_SetRelativeMouseMode( SDL_FALSE );
                            break;

                            case SDLK_f:
                            if ( state ) b_F = !b_F;
                            break;

                            default: ;
                        }
                        if (event.key.keysym.mod & KMOD_LSHIFT) {
                            camaraSpeed = 140.0;
                        } else if (event.key.keysym.mod & KMOD_LCTRL) {
                            camaraSpeed = 2.0;
                        } else {
                            camaraSpeed = 60.0;
                        }

                        //if (event.key.keysym.sym == SDLK_p && event.key.keysym.mod & KMOD_LALT) { }
                    } else if (event.type == SDL_MOUSEMOTION) {
                        if ( SDL_GetRelativeMouseMode() ) camera.onMouseMoved(  event.
                        motion.xrel, event.motion.yrel);
                    } else if (event.type == SDL_MOUSEBUTTONDOWN) {
                        if (event.button.button == SDL_BUTTON_LEFT) {
                            SDL_SetRelativeMouseMode( SDL_TRUE );
                        }
                    } else if (event.type == SDL_WINDOWEVENT) {
                        int w, h;
                        SDL_GetWindowSize( window, &w, &h );
                        GL.Viewport( 0, 0, w, h );
                    }
                    //else if (event.type == SDL_WINDOWEVENT_FOCUS_LOST) {
                    //	SDL_SetRelativeMouseMode(SDL_FALSE);
                    //}
                 */
        }

        void Mainloop(float delta) {
            this.window.SwapBuffers();
            GL.ClearColor( 1f, 0f, 1f, 1.0f );
            GL.Clear( ClearBufferMask.ColorBufferBit );
            GL.Clear( ClearBufferMask.DepthBufferBit );
            this.window.Title = delta.ToString();

            return;

            if ( b_W ) {
                camera.moveFront( delta * camaraSpeed );
            }

            if ( b_S ) {
                camera.moveFront( delta * -camaraSpeed );
            }

            if ( b_A ) {
                camera.moveSideways( delta * -camaraSpeed );
            }

            if ( b_D ) {
                camera.moveSideways( delta * camaraSpeed );
            }

            if ( b_Q ) {
                camera.moveUp( delta * camaraSpeed );
            }

            if ( b_E ) {
                camera.moveUp( -delta * camaraSpeed );
            }

            if ( b_F ) {
                spotColor = new Vector3( 2 );
            }
            else {
                spotColor = new Vector3( 0 );
            }

            GL.Uniform3( GL.GetUniformLocation( shader.GetShaderID(), "u_spot_light.diffuse" ),  ref spotColor );
            GL.Uniform3( GL.GetUniformLocation( shader.GetShaderID(), "u_spot_light.specular" ), ref spotColor );
            spotColor *= 0.1F;
            GL.Uniform3( GL.GetUniformLocation( shader.GetShaderID(), "u_spot_light.ambient" ), ref spotColor );
            camera.update();
            modelViewProj = camera.GetViewProj() * camModelRender;
            Matrix4 modelView = camera.GetView() * camModelRender;
            var     matrix4   = new OpenTK.Matrix4();
            matrix4.Transpose();
            Matrix4 invModelView            = new Matrix4(); //TODO: glm.transpose( inverse( modelView ) );
            Vector3 transformedSunDirection = new Vector3(); //TODO: transpose( inverse( camera.GetView() ) ) * vec4( sunDirection, 1.0F );
            GL.Uniform3( directionLocation, ref transformedSunDirection );
            Matrix4 pointLightMatrix = glm.rotate( new mat4( 1f ), -delta, new vec3( 0, 1, 0 ) ).M();
            pointLightPosition = pointLightMatrix * pointLightPosition;
            Vector3 transformedPointLightPosition = new Vector3(); //TODO:(Vector3) ( camera.GetView() * ( pointLightPosition ) );
            GL.Uniform3( pointLightLocation, ref transformedPointLightPosition );

            GL.UniformMatrix4( modelViewProjMatrixLocation, false, ref modelViewProj );
            GL.UniformMatrix4( modelViewLocation,           false, ref modelView );
            GL.UniformMatrix4( invModelViewLocation,        false, ref invModelView );

            ///
            for ( int i = 0; i < this.mats.Count; ++i ) {
                //mats[i]   =Matrix4.ro  rotate(mats[i],time * , Vector3 (0, time,0));
                modelViewProj = camera.GetViewProj() * mats[i];
                modelView     = camera.GetView()     * mats[i];

                invModelView = ( modelView.Inverted() );
                GL.UniformMatrix4( modelViewProjMatrixLocation, false, ref modelViewProj );
                GL.UniformMatrix4( modelViewLocation,           false, ref modelView );
                GL.UniformMatrix4( invModelViewLocation,        false, ref invModelView );

                //TODO:models[i] -> Render();
            }
        }

        private void PrintStatus(char c, int getShaderId, string shader, int i, object messageColor) { }

        private void GOnRenderFrame(object sender, FrameEventArgs e) { Mainloop( (float) e.Time ); }
    }
}
