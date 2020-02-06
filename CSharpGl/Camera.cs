using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlmNet;

namespace CSharpGl {
    class Camera {
        public Camera(float fov, float width, float height) {
            procjection = GlmNet.glm.perspective( fov / 2.0F, width / height, .1F, 1000.0F );
            view        = new mat4( 1.0F );
            position    = new vec3( 0.0F );
            update();
        }

        public virtual void update() { viewProj = procjection * view; }

        public virtual void Translate(vec3 v) {
            position += v;
            view     =  GlmNet.glm.translate( view, v * -1.0F );
        }

        public vec3 GetPosition() { return position; }

        public mat4 GetProcjection() { return procjection; }

        public mat4 GetView() { return view; }

        public mat4 GetViewProj() { return viewProj; }

        protected vec3 position;
        protected mat4 procjection;
        protected mat4 view;
        protected mat4 viewProj;

    }
}
