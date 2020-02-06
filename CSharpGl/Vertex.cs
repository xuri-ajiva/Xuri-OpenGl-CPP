using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlmNet;
using static GlmNet.glm;

namespace CSharpGl {

   public struct VertexTextureAndNormal {
        vec3 position;
        vec3 normal;
        vec3 tangent;
        vec2 textureCord;
    };

    public struct VertexTextureOnly {
        vec3 position;
        vec3 normal;
        vec2 textureCord;
    };


    public struct VertexMaterialOnly {
        vec3 position;
        vec3 normal;
    };

    public enum VertexMode {
        TextureAndNormal,
        TextureOnly,
        MaterialOnly
    };

}
