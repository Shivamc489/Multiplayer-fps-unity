using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace LunarCatsStudio.SuperCombiner {

	/// <summary>
	/// Combined result.
	/// </summary>
	public class CombinedResult : ScriptableObject {
		// The combined material
		public Material material;
		// The list of original materials
		[HideInInspector]
		public List<Material> originalMaterialList = new List<Material>();
		// The list of uvs
		public Rect[] uvs;
        public Rect[] uvs2;
		[HideInInspector]
		public List<float> scaleFactors = new List<float> ();
        // The list of mesh results
		public List<MeshCombined> meshResults = new List<MeshCombined>();
		// The number of materials combined
        public int materialCombinedCount;
        // The number of meshes combined
        public int meshesCombinedCount;
        // The number of skinnedMeshes combined
        public int skinnedMeshesCombinedCount;
        // The number of vertex in combined mesh
        public int totalVertexCount;
		// The number of submeshes
        public int subMeshCount;
        // The duration of the process
        public TimeSpan duration;

        /// <summary>
        /// Clear all combine data
        /// </summary>
        public void Clear()
        {
            material = null;
            if (originalMaterialList != null)
            {
                originalMaterialList.Clear();
            }
            uvs = null;
            uvs2 = null;
			scaleFactors.Clear ();
            materialCombinedCount = 0;
            meshesCombinedCount = 0;
            skinnedMeshesCombinedCount = 0;
            totalVertexCount = 0;
            subMeshCount = 0;
            meshResults.Clear();
        }

        /// <summary>
        /// Add a new CombinedMesh
        /// </summary>
        /// <param name="combinedMesh"></param>
        /// <param name="combineInstanceID"></param>
        public void AddCombinedMesh(Mesh combinedMesh, CombineInstanceID combineInstanceID)
        {
			MeshCombined meshResult = new MeshCombined ();

            int vertexIndex = 0;
            int triangleIndex = 0;
            for (int i = 0; i < combineInstanceID.combineInstances.Count; i++) {
				if(!meshResult.instanceIds.Contains(combineInstanceID.instancesID[i]))
                {
                    vertexIndex += combineInstanceID.combineInstances[i].mesh.vertexCount;
                    triangleIndex += combineInstanceID.combineInstances[i].mesh.triangles.Length;
                    meshResult.names.Add(combineInstanceID.names[i]);
                    meshResult.instanceIds.Add(combineInstanceID.instancesID[i]);
					meshResult.indexes.Add(new CombineInstanceIndexes(combineInstanceID.combineInstances[i].mesh, vertexIndex, triangleIndex));
                }
            }

            meshResults.Add(meshResult);
        }

		// Returns the index of a given material in the list
		public int FindCorrespondingMaterialIndex(Material matToFind) {
			if (originalMaterialList.Contains (matToFind)) {
				return originalMaterialList.IndexOf (matToFind);
			} else 
			{
				Debug.LogWarning ("[Super Combiner] Material " + matToFind + " was not found.");
				return 0;
			}
		}
	}

	/// <summary>
	/// Mesh result. 
	/// Each mesh is a part of the combined result.
	/// Either you combine only materials, then this is a list of new created meshes
	/// Or you combine meshes, they may be split if size exceeds 65k vertices
	/// </summary>
	[System.Serializable]
	public class MeshCombined {
		// List of name of the original game objects combined
        public List<string> names = new List<string>();
		// List of instance Id for each original game object combined
        public List<int> instanceIds = new List<int>();
		// List of indexes for combined meshes
        public List<CombineInstanceIndexes> indexes = new List<CombineInstanceIndexes>();

        /// <summary>
        /// Removes a mesh (given by the instanceID of the gameObject on which it is attached) from the combined mesh
        /// </summary>
        /// <param name="instanceID"></param>
		public Mesh RemoveMesh(int instanceID, Mesh mesh)
        {
            if (instanceIds.Contains(instanceID))
            {
                int index = instanceIds.IndexOf(instanceID);

                Vector3[] vertices = mesh.vertices;
                Vector3[] newVertices = new Vector3[mesh.vertexCount - indexes[index].vertexCount];
                int[] triangles = mesh.triangles;
                int[] newTriangles = new int[triangles.Length - indexes[index].triangleCount];
                Vector2[] uv = mesh.uv;
                Vector2[] newUv = new Vector2[newVertices.Length];
                Vector2[] uv2 = mesh.uv2;
                Vector2[] newUv2 = new Vector2[newVertices.Length];

                // Assign new vertices, uv and uv2
                for (int i=0; i< newVertices.Length; i++)
                {
                    if(i < indexes[index].firstVertexIndex)
                    {
                        newVertices[i] = vertices[i];
                        newUv[i] = uv[i];
                        newUv2[i] = uv2[i];
                    } else
                    {
                        newVertices[i] = vertices[i + indexes[index].vertexCount];
                        newUv[i] = uv[i + indexes[index].vertexCount];
                        newUv2[i] = uv2[i + indexes[index].vertexCount];
                    }
                }

                // Assign new triangles
                for (int i = 0; i < newTriangles.Length; i++)
                {
                    if (i < indexes[index].firstTriangleIndex)
                    {
                        newTriangles[i] = triangles[i];
                    }
                    else
                    {
                        newTriangles[i] = triangles[i + indexes[index].triangleCount] - indexes[index].vertexCount;
                    }
                }

                // Offset all vertices and triangles of meshes placed after the instanceID's mesh 
                for(int i=index; i< indexes.Count; i++)
                {
                    indexes[i].MoveIndexes(indexes[index].vertexCount, indexes[index].triangleCount);
                }
                // Delete the mesh from the list
                indexes.RemoveAt(index);
                instanceIds.RemoveAt(index);
                names.RemoveAt(index);

                // Reasign new vertices, triangles and uvs to the mesh
                mesh.Clear();
                mesh.vertices = new List<Vector3>(newVertices).ToArray();
                mesh.SetTriangles(newTriangles, 0);
                mesh.uv = newUv;
                mesh.uv2 = newUv2;
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
            }
			return mesh;
        }

    }

    /// <summary>
    /// The combine instance indexes of vertex and triangles
    /// </summary>
    [System.Serializable]
	public class CombineInstanceIndexes {
		// Index of the first vertex in mesh.vertices
		public int firstVertexIndex;
		// The vertexcount
		public int vertexCount;
		// Index of the first triangle in mesh.triangles
		public int firstTriangleIndex;
		// The trianglecount
		public int triangleCount;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="vertexIndex"></param>
        /// <param name="trianglesIndex"></param>
		public CombineInstanceIndexes(Mesh mesh, int vertexIndex, int trianglesIndex)
        {            
            vertexCount = mesh.vertexCount;
            firstVertexIndex = vertexIndex;
            triangleCount = mesh.triangles.Length;
            firstTriangleIndex = trianglesIndex;
        }

        /// <summary>
        /// Offset first indexes for vertices and triangles
        /// </summary>
        /// <param name="vertexOffset"></param>
        /// <param name="triangleOffset"></param>
        public void MoveIndexes(int vertexOffset_p, int triangleOffset_p)
        {
            firstVertexIndex -= vertexOffset_p;
            firstTriangleIndex -= triangleOffset_p;
        }
    }
}
