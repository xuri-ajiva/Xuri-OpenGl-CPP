#pragma once

struct VertexTextureAndNormal {
	glm::vec3 position;
	glm::vec3 normal;
	glm::vec3 tangent;
	glm::vec2 textureCord;
};

struct VertexTextureOnly {
	glm::vec3 position;
	glm::vec3 normal;
	glm::vec2 textureCord;
};


struct VertexMaterialOnly {
	glm::vec3 position;
	glm::vec3 normal;
};

enum VertexMode {
	TextureAndNormal,
	TextureOnly,
	MaterialOnly
};