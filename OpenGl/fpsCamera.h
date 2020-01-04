#pragma once
#include "Camera.h"

class FPSCamera : public Camera {
public:
	FPSCamera(float fov, float width, float height): Camera(fov, width, height) {
		up    = glm::vec3(0.0, 1.0, 0.0);
		yaw   = -90;
		pitch = 0;
		onMouseMoved(0.0, 0.0);
		FPSCamera::update();
	}

	void onMouseMoved(float xRel, float yRel) {
		yaw += xRel * mouseSensitivity;
		pitch -= yRel * mouseSensitivity;
		pitch = pitch > 89.0F ? 89.0F : pitch < -89.0F ? -89.0F : pitch;

		glm::vec3 front;
		front.x = cos(glm::radians(pitch)) * cos(glm::radians(yaw));
		front.y = sin(glm::radians(pitch));
		front.z = cos(glm::radians(pitch)) * sin(glm::radians(yaw));
		lockAt  = glm::normalize(front);
		update();
	}

	void update() override {
		view     = glm::lookAt(position, position + lockAt, up);
		viewProj = procjection * view;
	}

	void moveFront(float amount) {
		//								//// not move up and down
		Translate((!freeCam ? (glm::normalize(glm::vec3(1.0, .0, 1.0) * lockAt)) : lockAt) * amount);

		update();
	}

	void moveSideways(float amount) {
		Translate(glm::normalize(glm::cross(lockAt, up)) * amount);
		update();
	}

	void moveUp(float amount) {
		Translate(up * amount);
		update();
	}

	void SetFreeCam(bool value) {
		freeCam = value;
	}

	bool GetFreeCam() {
		return freeCam;
	}

protected:
	bool        freeCam = false;
	float       yaw;
	float       pitch;
	glm::vec3   lockAt {};
	const float mouseSensitivity = 0.3F;
	glm::vec3   up {};
};
