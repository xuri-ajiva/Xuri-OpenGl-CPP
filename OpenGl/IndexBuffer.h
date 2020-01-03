#pragma once

#include  <GL/glew.h>

#include "defines.h"
#include <cstddef>

struct IndexBuffer final {
	IndexBuffer(void* data, Uint32 numVertices, Uint8 elementSize);

	~IndexBuffer();

	void BIND() const;

	static void UNBIND();

	GLuint GET_BUFFER_ID() const;

private:
	GLuint BUFFER_ID_ {};
};

inline IndexBuffer::IndexBuffer(void* data, Uint32 numIndex, Uint8 elementSize) {
	glGenBuffers (1, &BUFFER_ID_);
	glBindBuffer (GL_ELEMENT_ARRAY_BUFFER, BUFFER_ID_);
	glBufferData (GL_ELEMENT_ARRAY_BUFFER, numIndex * elementSize, data, GL_STATIC_DRAW);
}

inline IndexBuffer::~IndexBuffer() {
	glDeleteBuffers (1, &BUFFER_ID_);
}

inline void IndexBuffer::BIND() const {
	glBindBuffer (GL_ELEMENT_ARRAY_BUFFER, BUFFER_ID_);
}

inline void IndexBuffer::UNBIND() {
	glBindBuffer (GL_ELEMENT_ARRAY_BUFFER, 0);
}

inline GLuint IndexBuffer::GET_BUFFER_ID() const {
	return BUFFER_ID_;
}
