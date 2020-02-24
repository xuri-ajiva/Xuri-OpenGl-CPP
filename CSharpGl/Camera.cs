using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlmNet;
using OpenTK;

namespace CSharpGl {
    class Camera {
        public Camera(float fov, float width, float height) {
            procjection = GlmNet.glm.perspective( fov / 2.0F, width / height, .1F, 1000.0F ).M();
            view        = new mat4( 1.0F ).M();
            position    = new Vector3( 0.0F );
            update();
        }

        public virtual void update() { viewProj = procjection * view; }

        public virtual void Translate(Vector3 v) {
            position += v;
            view     =  GlmNet.glm.translate( view.M(), v.V() * -1.0F ).M();
        }

        public Vector3 GetPosition() { return position; }

        public Matrix4 GetProcjection() { return procjection; }

        public Matrix4 GetView() { return view; }

        public Matrix4 GetViewProj() { return viewProj; }

        protected Vector3 position;
        protected Matrix4 procjection;
        protected Matrix4 view;
        protected Matrix4 viewProj;

    }
}
