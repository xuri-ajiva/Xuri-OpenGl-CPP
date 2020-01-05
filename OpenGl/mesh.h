#pragma once
#include <vector>
#include <fstream>

#include "defines.h"

#include "libs/glm/glm.hpp"
#include "Shader.h"
#include "VertexBuffer.h"
#include "IndexBuffer.h"

struct Material {
	glm::vec3 diffuse   = {};
	glm::vec3 specular  = {};
	glm::vec3 emissive  = {};
	float     shininess = 0.0F;

	Material() = default;

	Material(const glm::vec3& diffuse, const glm::vec3& specular, const glm::vec3& emissive,
	         float            shininess) : diffuse(diffuse),
	                                       specular(specular),
	                                       emissive(emissive),
	                                       shininess(shininess) {}

	Material(const glm::vec3& diffuse, const glm::vec3& specular, float shininess) : diffuse(diffuse),
	                                                                                 specular(specular),
	                                                                                 shininess(shininess) {}
};

class Mesh {
public:
	Mesh(std::vector<Vertex> vertices, Uint64  numVertices, std::vector<Uint32> indices, Uint64 numIndices,
	     Material            material, Shader* shader) {
		this->material   = material;
		this->shader     = shader;
		this->numIndices = numIndices;


		vertexBuffer = new VertexBuffer(vertices.data(), numVertices);
		std::cout << "vertexBuffer initialized PointerID: " << vertexBuffer->GET_BUFFER_ID() << std::endl;
		indexBuffer = new IndexBuffer(indices.data(), numIndices, sizeof(indices[0]));
		std::cout << "indexBuffer initialized PointerID: " << indexBuffer->GET_BUFFER_ID() << std::endl;

		diffuseLocation   = glGetUniformLocation(shader->GetShaderID(), "u_diffuse");
		specularLocation  = glGetUniformLocation(shader->GetShaderID(), "u_specular");
		emissiveLocation  = glGetUniformLocation(shader->GetShaderID(), "u_emissive");
		shininessLocation = glGetUniformLocation(shader->GetShaderID(), "u_shininess");
	}

	~Mesh() {
		delete vertexBuffer;
		delete indexBuffer;
	}

	inline void Render() {
		vertexBuffer->BIND();
		indexBuffer->BIND();
		glUniform3fv(diffuseLocation, 1, (float*)&material.diffuse.x);
		glUniform3fv(specularLocation, 1, (float*)&material.specular.x);
		glUniform3fv(emissiveLocation, 1, (float*)&material.emissive.x);
		glUniform1f(shininessLocation, material.shininess);
		glDrawElements(GL_TRIANGLES, numIndices, GL_UNSIGNED_INT, 0);
		vertexBuffer->UNBIND();
		indexBuffer->UNBIND();
	}

private:
	VertexBuffer* vertexBuffer;
	IndexBuffer*  indexBuffer;
	Shader*       shader;
	Material      material;
	Uint64        numIndices = 0;
	int           diffuseLocation;
	int           specularLocation;
	int           emissiveLocation;
	int           shininessLocation;
};

class Model {
public:
	void Init(const char* filename, Shader* shader) {
		std::ifstream input = std::ifstream(filename, std::ios::in | std::ios::binary);
		if (!input.is_open()) {
			std::cout << "File Not Found!" << std::endl;
			return;
		}
		UINT64 numMeshes;
		input.read((char*)&numMeshes, sizeof(Uint64));

		meshes.reserve(numMeshes);

		for (Uint64 i = 0; i < numMeshes; i++) {
			Material material;

			std::vector<Uint32> indices;
			std::vector<Vertex> vertices;

			Uint64 numVertices = 0;
			Uint64 numIndices  = 0;

			input.read((char*)&material, sizeof(Material));


			input.read((char*)&numVertices, sizeof(Uint64));
			input.read((char*)&numIndices, sizeof(Uint64));

			for (Uint64 i = 0; i < numVertices; i++) {
				Vertex vertex;
				input.read((char*)&vertex.position.x, sizeof(float));
				input.read((char*)&vertex.position.y, sizeof(float));
				input.read((char*)&vertex.position.z, sizeof(float));
				input.read((char*)&vertex.normal.x, sizeof(float));
				input.read((char*)&vertex.normal.y, sizeof(float));
				input.read((char*)&vertex.normal.z, sizeof(float));
				vertices.push_back(vertex);
			}
			for (Uint64 i = 0; i < numIndices; i++) {
				Uint32 index;
				input.read((char*)&index, sizeof(Uint32));
				indices.push_back(index);
			}

			Mesh* mesh = new Mesh(vertices, numVertices, indices, numIndices, material, shader);
			meshes.push_back(mesh);
		}
	}

	void Render() {

		for (Mesh* mesh : meshes) {
			mesh->Render();
		}
	}

	~Model() {
		for (Mesh* mesh : meshes) {
			delete mesh;
		}
	}

private:
	std::vector<Mesh*> meshes;
};
