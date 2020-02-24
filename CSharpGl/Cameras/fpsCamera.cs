using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlmNet;
using OpenTK;
using static GlmNet.glm;

namespace CSharpGl.Cameras {
    class FPSCamera : Camera {
        public FPSCamera(float fov, float width, float height) : base( fov, width, height ) {
            up    = new Vector3( 0.0f, 1.0f, 0.0f );
            yaw   = -90;
            pitch = 0;
            onMouseMoved( 0.0f, 0.0f );
            base.update();
        }

        public void onMouseMoved(float xRel, float yRel) {
            yaw   += xRel * mouseSensitivity;
            pitch -= yRel * mouseSensitivity;
            pitch = pitch > 89.0F
                ? 89.0F
                : pitch < -89.0F
                    ? -89.0F
                    : pitch;

            Vector3 front;
            front.X = cos( radians( pitch ) ) * cos( radians( yaw ) );
            front.Y = sin( radians( pitch ) );
            front.Z = cos( radians( pitch ) ) * sin( radians( yaw ) );
            lockAt  = Vector3.Normalize( front );
            update();
        }

        public override void update() {
            view = Matrix4.LookAt( position, position + lockAt, up );
            viewProj = procjection * view;
        }

        public void moveFront(float amount) {
            //								//// not move up and down
            Translate( ( ( !freeCam ) ? ( Vector3.Normalize( new Vector3( 1.0f, .0f, 1.0f ) * lockAt ) ) : lockAt ) * amount );

            update();
        }

        public void moveSideways(float amount) {
            Translate( Vector3.Normalize(  Vector3.Cross( lockAt, up ) ) * amount );
            update();
        }

        public void moveUp(float amount) {
            Translate( up * amount );
            update();
        }

        public void SetFreeCam(bool value) { freeCam = value; }

        public bool GetFreeCam() { return freeCam; }

        bool          freeCam = false;
        float         yaw;
        float         pitch;
        private Vector3 lockAt;
        const   float mouseSensitivity = 0.3F;
        private Vector3  up;

    }
}
