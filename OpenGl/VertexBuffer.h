#pragma once

#include  <GL/glew.h>

#include "defines.h"
#include <cstddef>

struct VertexBuffer final {
	VertexBuffer(void* data, Uint32 numVertices, VertexMode mode);

	~VertexBuffer();

	void BIND() const;

	static void UNBIND();

	GLuint GET_BUFFER_ID() const;

private:
	GLuint BUFFER_ID_ {};

	GLuint VAO_ {};
};

inline VertexBuffer::VertexBuffer(void* data, Uint32 numVertices, VertexMode mode) {
	glGenVertexArrays(1, &VAO_);
	glBindVertexArray(VAO_);
	int size = 0;
	switch (mode) {
		case TextureAndNormal: size = sizeof(VertexTextureAndNormal);
			break;
		case TextureOnly: size = sizeof(VertexTextureOnly);
			break;
		case MaterialOnly: size = sizeof(VertexMaterialOnly);
			break;
		default: break;
	}
	
	//bind all to vio **-

	glGenBuffers(1, &BUFFER_ID_);
	glBindBuffer(GL_ARRAY_BUFFER, BUFFER_ID_);
	glBufferData(GL_ARRAY_BUFFER, numVertices * size, data, GL_STATIC_DRAW);


	glEnableVertexAttribArray(0);
	glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, size, (void*)offsetof(struct VertexMaterialOnly, position));
	glEnableVertexAttribArray(1);
	glVertexAttribPointer(1, 3, GL_FLOAT, GL_FALSE, size, (void*)offsetof(struct VertexMaterialOnly, normal));

	if (mode == TextureAndNormal) {
		glEnableVertexAttribArray(2);
		glVertexAttribPointer(2, 3, GL_FLOAT, GL_FALSE, size, (void*)offsetof(struct VertexTextureAndNormal, tangent));
		glEnableVertexAttribArray(3);
		glVertexAttribPointer(3, 2, GL_FLOAT, GL_FALSE, size, (void*)offsetof(struct VertexTextureAndNormal, textureCord));
	}
	else if (mode == TextureOnly) {
		glEnableVertexAttribArray(2);
		glVertexAttribPointer(3, 2, GL_FLOAT, GL_FALSE, size, (void*)offsetof(struct VertexTextureOnly, textureCord));
	}

	//bind all to vio -**

	glBindVertexArray(0);
}

inline VertexBuffer::~VertexBuffer() {
	glDeleteBuffers(1, &BUFFER_ID_);
}

inline void VertexBuffer::BIND() const {
	glBindVertexArray(VAO_);
}

inline void VertexBuffer::UNBIND() {
	glBindVertexArray(0);
}

inline GLuint VertexBuffer::GET_BUFFER_ID() const {
	return BUFFER_ID_;
}
