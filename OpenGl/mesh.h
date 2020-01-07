#pragma once
#include <vector>
#include <fstream>

#include "defines.h"

#include "libs/glm/glm.hpp"
#include "Shader.h"
#include "VertexBuffer.h"
#include "IndexBuffer.h"
#include "libs/stb_image.h"

struct BMFMaterial {
	glm::vec3 diffuse   = {};
	glm::vec3 specular  = {};
	glm::vec3 emissive  = {};
	float     shininess = 0.0F;
};

struct Material {
	BMFMaterial material   = {};
	GLuint      diffuseMap = {};
	GLuint      normalMap  = {};
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

		diffuseLocation   = glGetUniformLocation(shader->GetShaderID(), "u_material.diffuse");
		specularLocation  = glGetUniformLocation(shader->GetShaderID(), "u_material.specular");
		emissiveLocation  = glGetUniformLocation(shader->GetShaderID(), "u_material.emissive");
		shininessLocation = glGetUniformLocation(shader->GetShaderID(), "u_material.shininess");
		diffuseMapLocation = glGetUniformLocation(shader->GetShaderID(), "u_diffuse_map");
	}

	~Mesh() {
		delete vertexBuffer;
		delete indexBuffer;
	}

	void Render() {
		vertexBuffer->BIND();
		indexBuffer->BIND();
		glUniform3fv(diffuseLocation, 1, (float*)&material.material.diffuse.x);
		glUniform3fv(specularLocation, 1, (float*)&material.material.specular.x);
		glUniform3fv(emissiveLocation, 1, (float*)&material.material.emissive.x);
		glUniform1f(shininessLocation, material.material.shininess);		
        glBindTexture(GL_TEXTURE_2D, material.diffuseMap);
        glUniform1i(diffuseMapLocation, 0);
		glDrawElements(GL_TRIANGLES, numIndices, GL_UNSIGNED_INT, 0);
		//vertexBuffer->UNBIND();
		//indexBuffer->UNBIND();
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
	int           diffuseMapLocation;
};

class Model {
public:
	void Init(const char* filename, Shader* shader) {
		std::ifstream input        = std::ifstream(filename, std::ios::in | std::ios::binary);
		UINT64        numMeshes    = 0;
		UINT64        numMaterials = 0;
		if (!input.is_open()) {
			std::cout << "File Not Found!" << std::endl;
			return;
		}

		input.read((char*)&numMaterials, sizeof(Uint64));
		for (Uint64 i = 0; i < numMaterials; i++) {
			Material material = {};
			input.read((char*)&material, sizeof(BMFMaterial));

			Uint64 diffuseMapNameLength = 0;
			input.read((char*)&diffuseMapNameLength, sizeof(Uint64));
			std::string diffuseMapName(diffuseMapNameLength, '\0');
			input.read((char*)&diffuseMapName[0], diffuseMapNameLength);
			std::cout << "diffuseMap: " << diffuseMapName << std::endl;

			Uint64 normalMapNameLength = 0;
			input.read((char*)&normalMapNameLength, sizeof(Uint64));
			std::string normalMapName(normalMapNameLength, '\0');
			input.read((char*)&normalMapName[0], normalMapNameLength);
			std::cout << "normalMap: " << normalMapName << std::endl;

			assert(diffuseMapNameLength > 0);
			assert(normalMapNameLength > 0);

			Int32 textureWidth  = 0;
			Int32 textureHeight = 0;
			Int32 bitsPerPixel  = 0;
			glGenTextures(2, &material.diffuseMap);
			stbi_set_flip_vertically_on_load(true);
			{
				auto textureBuffer = stbi_load(diffuseMapName.c_str(), &textureWidth, &textureHeight, &bitsPerPixel, 4);
				assert(textureBuffer);
				assert(material.diffuseMap);

				glBindTexture(GL_TEXTURE_2D, material.diffuseMap);

				glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
				glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
				glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
				glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);

				glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, textureWidth, textureHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, textureBuffer);

				if (textureBuffer) {
					stbi_image_free(textureBuffer);
				}
			}

			{
				auto textureBuffer = stbi_load(normalMapName.c_str(), &textureWidth, &textureHeight, &bitsPerPixel, 4);
				assert(textureBuffer);
				assert(material.normalMap);

				glBindTexture(GL_TEXTURE_2D, material.normalMap);

				glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
				glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
				glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
				glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);

				glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, textureWidth, textureHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, textureBuffer);

				if (textureBuffer) {
					stbi_image_free(textureBuffer);
				}
			}

			glBindTexture(GL_TEXTURE_2D, 0);
			materials.push_back(material);
		}
		// mesh
		input.read((char*)&numMeshes, sizeof(Uint64));
		meshes.reserve(numMeshes);
		for (Uint64 i = 0; i < numMeshes; i++) {
			std::vector<Uint32> indices;
			std::vector<Vertex> vertices;

			Uint64 numVertices   = 0;
			Uint64 numIndices    = 0;
			Uint64 materialIndex = 0;

			input.read((char*)&materialIndex, sizeof(Uint64));
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
				input.read((char*)&vertex.textureCord.x, sizeof(float));
				input.read((char*)&vertex.textureCord.y, sizeof(float));
				vertices.push_back(vertex);
			}
			for (Uint64 i = 0; i < numIndices; i++) {
				Uint32 index;
				input.read((char*)&index, sizeof(Uint32));
				indices.push_back(index);
			}

			Mesh* mesh = new Mesh(vertices, numVertices, indices, numIndices, materials[materialIndex], shader);
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
	std::vector<Mesh*>    meshes;
	std::vector<Material> materials;
};
