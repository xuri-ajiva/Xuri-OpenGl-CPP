using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace CSharpGl {
    class Shader {

        int shaderId;

      public  Shader(string vertexShaderFilename, string fragmentShaderFilename) {
            shaderId = createShader( vertexShaderFilename, fragmentShaderFilename );
        }

      ~Shader() {
          //GL.DeleteProgram( shaderId );
      }

        public void bind() { GL.UseProgram( shaderId ); }

        void unbind() { GL.UseProgram( 0 ); }

        public int GetShaderID() { return shaderId; }

        int compile(string shaderSource, ShaderType type) {
            int id = GL.CreateShader( type );
            GL.ShaderSource( id, shaderSource );
            GL.CompileShader( id );

            string result = GL.GetShaderInfoLog( id );

            Console.WriteLine( "Shader Compile: " + result );

            return id;
        }

        string parse(string filename) { return File.ReadAllText( filename ); }

        int createShader(string vertexShaderFilename, string fragmentShaderFilename) {
            string vertexShaderSource   = parse( vertexShaderFilename );
            string fragmentShaderSource = parse( fragmentShaderFilename );

            int program = GL.CreateProgram();
            int vs      = compile( vertexShaderSource, ShaderType.VertexShader );
            int fs      = compile( fragmentShaderSource, ShaderType.FragmentShader );

            GL.AttachShader( program, vs );
            GL.AttachShader( program, fs );
            GL.LinkProgram( program );

            /*// del shader s // after link can all shader`s be deleted but do not del program
            GL.DetachShader(program, vs);
            GL.DetachShader(program, fs);
        
            GL.DeleteShader(vs);
            GL.DeleteShader(fs);
            //*/

            return program;
        }
    }
}
