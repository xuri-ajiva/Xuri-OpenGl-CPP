using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSharpGl.Cameras;
using GlmNet;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace CSharpGl {
    public static class exth {

        //public static Vector3 V(this Vector3 v) { return new Vector3( v.x, v.y, v.z ); }
    }
    class Program {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );
            var p = new Program();
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


        Vector3 sunDirection;
        Vector3 sunColor;
        int     directionLocation;
        Vector3 spotColor;
        Shader  shader;
        vec4    pointLightPosition;
        int     pointLightLocation;

        private int run() {
            //Application.Run(new Form1());
            var g = new OpenTK.GameWindow( 1000, 1000, GraphicsMode.Default, "Xuri´s OpenGl C#" );
            g.RenderFrame += GOnRenderFrame;

            int init_err = Setup.Init();

            if ( init_err != 0 ) {
                return init_err;
            }

            Console.WriteLine( "OpenGl version: " + GL.GetString( StringName.Version ) );

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
            pointLightPosition = new vec4( 1, 1, 10, 1 );
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
            string ext  = ( "bmf" );
            int    idx  = 0;
            /*
            foreach ( var p in new DirectoryInfo(path).GetFiles())
            {
                if(p.Extension == ext)
                    Console.WriteLine( "Loading: " + p + '\n');
                    var    t     = p.FullName;
                    Model model = new Model( t, &shader );
                    model . Init();
                    models.push_back( model );
                    var mat4X = mat4( 1.0F );
                    mat4X = translate( mat4X, Vector3( 100 * ( idx++ ), 0, 0 ) );
                    mat4X = scale( mat4X, Vector3( .2 ) );
                    mats.push_back( mat4X );
                }
            }  */

            camModelRender = glm.scale( new mat4( 1.0F ), new vec3( .2f ) );

            //model_rotate      = scale(model_rotate, Vector3(.1F));

            camera = new FPSCamera( 90.0F, 1000, 800 );
            camera.Translate( new vec3( 0, 0, 5.0f ) );
            camera.update();

            modelViewProj = camera.GetViewProj() * camModelRender;

            modelViewProjMatrixLocation = GL.GetUniformLocation( this.shader.GetShaderID(), "u_modelViewProj" );

            modelViewLocation    = GL.GetUniformLocation( this.shader.GetShaderID(), "u_modelView" );
            invModelViewLocation = GL.GetUniformLocation( this.shader.GetShaderID(), "u_invModelView" );

            //GL.PolygonMode (GL_FRONT_AND_BACK,GL_LINE);

            float camaraSpeed = 60.0F;

            GL.Enable( EnableCap.DepthTest );

            return 0;
        }

        int           invModelViewLocation;
        int           modelViewLocation;
        int           modelViewProjMatrixLocation;
        mat4          camModelRender;
        FPSCamera     camera;
        mat4          modelViewProj;
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
            GL.ClearColor( 0.1f, 0.1f, 0.1f, 1.0f );
            GL.Clear( ClearBufferMask.ColorBufferBit );
            GL.Clear( ClearBufferMask.DepthBufferBit );

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
            mat4 modelView    = camera.GetView() * camModelRender;
            mat4 invModelView = new mat4(); //TODO: glm.transpose( inverse( modelView ) );

            Vector3 transformedSunDirection = new Vector3(); //TODO: transpose( inverse( camera.GetView() ) ) * vec4( sunDirection, 1.0F );
            GL.Uniform3( directionLocation, ref transformedSunDirection );

            mat4 pointLightMatrix = glm.rotate( new mat4( 1 ), -delta, new vec3( 0, 1, 0 ) );

            pointLightPosition = pointLightMatrix * pointLightPosition;
            Vector3 transformedPointLightPosition = new Vector3(); //TODO:(Vector3) ( camera.GetView() * ( pointLightPosition ) );
            GL.Uniform3( pointLightLocation, ref transformedPointLightPosition );

            var vec4 = this.modelViewProj[0];
            var f    = vec4[0];
            GL.UniformMatrix4( modelViewProjMatrixLocation, 1, false, ref f );
            var vec5 = modelView[0];
            var g    = vec5[0];
            GL.UniformMatrix4( modelViewLocation, 1, false, ref g );
            var vec6 = invModelView[0];
            var h    = vec6[0];
            GL.UniformMatrix4( invModelViewLocation, 1, false, ref h );

            ///

            for ( int i = 0; i < 0 /*TODO: idx*/; ++i ) {
                //mats[i]   =  rotate(mats[i],time * , Vector3 (0, time,0));
                modelViewProj = camera.GetViewProj(); //TODO:* mats[i];
                modelView     = camera.GetView();     //TODO: * mats[i];
                invModelView  = new mat4();           //TODO: transpose( inverse( modelView ) );

                GL.UniformMatrix4( modelViewProjMatrixLocation, 1, false, ref f );
                GL.UniformMatrix4( modelViewLocation,           1, false, ref g );
                GL.UniformMatrix4( invModelViewLocation,        1, false, ref h );

                //TODO:models[i] -> Render();
            }
        }


        private void PrintStatus(char c, int getShaderId, string shader, int i, object messageColor) { }

        private void GOnRenderFrame(object sender, FrameEventArgs e) { }


    }
}
