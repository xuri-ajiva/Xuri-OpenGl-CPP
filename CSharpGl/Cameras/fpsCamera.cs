using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlmNet;
using static GlmNet.glm;

namespace CSharpGl.Cameras {
    class FPSCamera : Camera {
        public FPSCamera(float fov, float width, float height) : base( fov, width, height ) {
            up    = new vec3( 0.0f, 1.0f, 0.0f );
            yaw   = -90;
            pitch = 0;
            onMouseMoved( 0.0f, 0.0f );
            base.update();
        }

        void onMouseMoved(float xRel, float yRel) {
            yaw   += xRel * mouseSensitivity;
            pitch -= yRel * mouseSensitivity;
            pitch = pitch > 89.0F
                ? 89.0F
                : pitch < -89.0F
                    ? -89.0F
                    : pitch;

            vec3 front;
            front.x = cos( radians( pitch ) ) * cos( radians( yaw ) );
            front.y = sin( radians( pitch ) );
            front.z = cos( radians( pitch ) ) * sin( radians( yaw ) );
            lockAt  = normalize( front );
            update();
        }

        public override void update() {
            view     = lookAt( position, position + lockAt, up );
            viewProj = procjection * view;
        }

        public void moveFront(float amount) {
            //								//// not move up and down
            Translate( ( ( !freeCam ) ? ( normalize( new vec3( 1.0f, .0f, 1.0f ) * lockAt ) ) : lockAt ) * amount );

            update();
        }

        public void moveSideways(float amount) {
            Translate( normalize( cross( lockAt, up ) ) * amount );
            update();
        }

        public void moveUp(float amount) {
            Translate( up * amount );
            update();
        }

        void SetFreeCam(bool value) { freeCam = value; }

        bool GetFreeCam() { return freeCam; }

        bool          freeCam = false;
        float         yaw;
        float         pitch;
        private vec3  lockAt;
        const   float mouseSensitivity = 0.3F;
        private vec3  up;

    }
}
