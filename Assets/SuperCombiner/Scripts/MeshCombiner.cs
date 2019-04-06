using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LunarCatsStudio.SuperCombiner;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This class manage the combine process of meshes and skinned meshes
/// </summary>
namespace LunarCatsStudio.SuperCombiner {
	public class MeshCombiner {

		// The maximum vertices count
		private int maxVerticesCount = 65534;
		// List of meshes UV bound
		private List<Rect> meshUVBounds = new List<Rect>();
		// The session name
		private string sessionName = "";
		// List of Blendshape frames
		private Dictionary<string, BlendShapeFrame> blendShapes = new Dictionary<string, BlendShapeFrame>();

		private int vertexOffset = 0;

        // The combinedResult reference
        private CombinedResult combinedResult;
        public CombinedResult CombinedResult
        {
            set
            {
                combinedResult = value;
            }
        }

        public void SetParameters(int maxVertices_p, string sessionName_p) {
			maxVerticesCount = maxVertices_p;
			sessionName = sessionName_p;
		}

		public void AddMeshUVBound(Rect uvBound) {
			meshUVBounds.Add (uvBound);
		}

		public void Clear() {
			meshUVBounds.Clear ();
			blendShapes.Clear ();
			vertexOffset = 0;
		}

        public List<GameObject> CombineToMeshes(List<MeshRenderer> meshRenderers, List<SkinnedMeshRenderer> skinnedMeshRenderers, Transform parent)
        {
            // The list of Meshes created
            List<GameObject> combinedMeshes = new List<GameObject>();
            // The list of combineInstances
            CombineInstanceID combineInstances = new CombineInstanceID();

            int verticesCount = 0;
            int combinedGameObjectCount = 0;
            
            // Process meshes for combine process
            for (int i = 0; i < meshRenderers.Count; i++)
            {
                // Get a copy of this mesh
                Mesh newMesh = copyMesh(meshRenderers[i].GetComponent<MeshFilter>().sharedMesh, meshRenderers[i].GetInstanceID().ToString());

                verticesCount += meshRenderers[i].GetComponent<MeshFilter>().sharedMesh.vertexCount;
                if (verticesCount > maxVerticesCount)
                {
                    combinedMeshes.Add(CreateCombinedMeshGameObject(combineInstances, parent, combinedGameObjectCount));
                    combineInstances.Clear();
                    combinedGameObjectCount++;
                    verticesCount = meshRenderers[i].GetComponent<MeshFilter>().sharedMesh.vertexCount;
                }

                // Create the list of CombineInstance for this mesh
                Matrix4x4 matrix = parent.transform.worldToLocalMatrix * meshRenderers[i].transform.localToWorldMatrix;
                combineInstances.AddRange(createCombinedInstances(newMesh, meshRenderers[i].sharedMaterials, meshRenderers[i].gameObject.GetInstanceID(), meshRenderers[i].gameObject.name, matrix));
            }

            for (int i = 0; i < skinnedMeshRenderers.Count; i++)
            {
                // Get a snapshot of the skinnedMesh renderer 
                Mesh mesh = copyMesh(skinnedMeshRenderers[i].sharedMesh, skinnedMeshRenderers[i].GetInstanceID().ToString());
                vertexOffset += mesh.vertexCount;

                verticesCount += skinnedMeshRenderers[i].sharedMesh.vertexCount;
                if (verticesCount > maxVerticesCount && combineInstances.Count() > 0)
                {
                    combinedMeshes.Add(CreateCombinedMeshGameObject(combineInstances, parent, combinedGameObjectCount));
                    combineInstances.Clear();
                    combinedGameObjectCount++;
                    verticesCount = skinnedMeshRenderers[i].sharedMesh.vertexCount;
                }

                // Create the list of CombineInstance for this skinnedMesh
                Matrix4x4 matrix = parent.transform.worldToLocalMatrix * skinnedMeshRenderers[i].transform.localToWorldMatrix;
                combineInstances.AddRange(createCombinedInstances(mesh, skinnedMeshRenderers[i].sharedMaterials, skinnedMeshRenderers[i].GetInstanceID(), skinnedMeshRenderers[i].gameObject.name, matrix));
            }


            if (combineInstances.Count() > 0)
            {
                // Create the combined GameObject which contains the combined meshes
                combinedMeshes.Add(CreateCombinedMeshGameObject(combineInstances, parent, combinedGameObjectCount));                
            }

            return combinedMeshes;
        }

        public List<GameObject> CombineToSkinnedMeshes(List<MeshRenderer> meshRenderers, List<SkinnedMeshRenderer> skinnedMeshRenderers, Transform parent)
        {
            // The list of Meshes created
            List<GameObject> combinedMeshes = new List<GameObject>();
            // The list of combineInstances
            CombineInstanceID combineInstances = new CombineInstanceID();

            int verticesCount = 0;
            int combinedGameObjectCount = 0;

            /*
			/ Skinned mesh parameters
			*/
            // List of bone weight
            List<BoneWeight> boneWeights = new List<BoneWeight>();
            // List of bones
            List<Transform> bones = new List<Transform>();
            // List of bindposes
            List<Matrix4x4> bindposes = new List<Matrix4x4>();
            // List of original bones mapped to their instanceId
            Dictionary<int, Transform> originalBones = new Dictionary<int, Transform>();
            // Link original bone instanceId to the new created bones
            Dictionary<int, Transform> originToNewBoneMap = new Dictionary<int, Transform>();
            // The vertices count
            int boneOffset = 0;

            // Get bones hierarchies from all skinned mesh
            for (int i = 0; i < skinnedMeshRenderers.Count; i++)
            {
                foreach (Transform t in skinnedMeshRenderers[i].bones)
                {
                    if (!originalBones.ContainsKey(t.GetInstanceID()))
                    {
                        originalBones.Add(t.GetInstanceID(), t);
                    }
                }
            }

            // Find the root bones
            Transform[] rootBones = FindRootBone(originalBones);
            for (int i = 0; i < rootBones.Length; i++)
            {
                // Instantiate the GameObject parent for this rootBone
                GameObject rootBoneParent = new GameObject("rootBone" + i);
                rootBoneParent.transform.position = rootBones[i].position;
                rootBoneParent.transform.parent = parent;
                rootBoneParent.transform.localPosition -= rootBones[i].localPosition;
                rootBoneParent.transform.localRotation = Quaternion.identity;

                // Instanciate a copy of the root bone
                GameObject newRootBone = InstantiateCopy(rootBones[i].gameObject);
                newRootBone.transform.position = rootBones[i].position;
                newRootBone.transform.rotation = rootBones[i].rotation;
                newRootBone.transform.parent = rootBoneParent.transform;
                newRootBone.AddComponent<MeshRenderer>();

                // Get the correspondancy map between original bones and new bones
                GetOrignialToNewBonesCorrespondancy(rootBones[i], newRootBone.transform, originToNewBoneMap);
            }

            // Copy Animator Controllers to new Combined GameObject
            foreach (Animator anim in parent.parent.GetComponentsInChildren<Animator>())
            {
                Transform[] children = anim.GetComponentsInChildren<Transform>();
                // Find the transform into which a copy of the Animator component will be added
                Transform t = FindTransformForAnimator(children, rootBones, anim);
                if (t != null)
                {
                    CopyAnimator(anim, originToNewBoneMap[t.GetInstanceID()].parent.gameObject);
                }
            }

            for (int i = 0; i < skinnedMeshRenderers.Count; i++)
            {
                // Get a snapshot of the skinnedMesh renderer 
                Mesh mesh = copyMesh(skinnedMeshRenderers[i].sharedMesh, skinnedMeshRenderers[i].GetInstanceID().ToString());
                vertexOffset += mesh.vertexCount;

                verticesCount += skinnedMeshRenderers[i].sharedMesh.vertexCount;
                if (verticesCount > maxVerticesCount && combineInstances.Count() > 0)
                {
                    // Create the new GameObject
                    GameObject combinedGameObject = createCombinedSkinnedMeshGameObject(combineInstances, parent, combinedGameObjectCount);                    
                    // Assign skinnedMesh parameters values
                    SkinnedMeshRenderer sk = combinedGameObject.GetComponent<SkinnedMeshRenderer>();
                    AssignParametersToSkinnedMesh(sk, bones, boneWeights, bindposes);
                    combinedMeshes.Add(combinedGameObject);
                    boneOffset = 0;

                    combineInstances.Clear();
                    combinedGameObjectCount++;
                    verticesCount = skinnedMeshRenderers[i].sharedMesh.vertexCount;
                }


                // Copy bone weights
                BoneWeight[] meshBoneweight = skinnedMeshRenderers[i].sharedMesh.boneWeights;
                foreach (BoneWeight bw in meshBoneweight)
                {
                    BoneWeight bWeight = bw;
                    bWeight.boneIndex0 += boneOffset;
                    bWeight.boneIndex1 += boneOffset;
                    bWeight.boneIndex2 += boneOffset;
                    bWeight.boneIndex3 += boneOffset;

                    boneWeights.Add(bWeight);
                }
                boneOffset += skinnedMeshRenderers[i].bones.Length;

                // Copy bones and bindposes
                Transform[] meshBones = skinnedMeshRenderers[i].bones;
                foreach (Transform bone in meshBones)
                {
                    bones.Add(originToNewBoneMap[bone.GetInstanceID()]);
                    bindposes.Add(bone.worldToLocalMatrix * parent.transform.localToWorldMatrix);
                }

                // Create the list of CombineInstance for this skinnedMesh
                Matrix4x4 matrix = parent.transform.worldToLocalMatrix * skinnedMeshRenderers[i].transform.localToWorldMatrix;
                combineInstances.AddRange(createCombinedInstances(mesh, skinnedMeshRenderers[i].sharedMaterials, skinnedMeshRenderers[i].GetInstanceID(), skinnedMeshRenderers[i].gameObject.name, matrix));
            }

            if (combineInstances.Count() > 0)
            {
                // Create the combined GameObject which contains the combined meshes
                // Create the new GameObject
                GameObject combinedGameObject = createCombinedSkinnedMeshGameObject(combineInstances, parent, combinedGameObjectCount);
                // Assign skinnedMesh parameters values
                SkinnedMeshRenderer sk = combinedGameObject.GetComponent<SkinnedMeshRenderer>();
                AssignParametersToSkinnedMesh(sk, bones, boneWeights, bindposes);
                combinedMeshes.Add(combinedGameObject);
            }

            return combinedMeshes;
        }

		// Assign the parameters of the new skinnedMesh
		private void AssignParametersToSkinnedMesh(SkinnedMeshRenderer skin, List<Transform> bones, List<BoneWeight> boneWeights, List<Matrix4x4> bindposes) {
			// Complete bone weights list if some are missing
			for (int i = boneWeights.Count; i < skin.sharedMesh.vertexCount; i++) {
				boneWeights.Add (boneWeights [0]);
			}

			skin.bones = bones.ToArray ();
			//sk.rootBone = newRootBone.transform;
			skin.sharedMesh.boneWeights = boneWeights.ToArray ();
			skin.sharedMesh.bindposes = bindposes.ToArray ();
			skin.sharedMesh.RecalculateBounds ();
			skin.sharedMesh.RecalculateNormals ();
			
			bones.Clear ();
			boneWeights.Clear ();
			bindposes.Clear ();
			vertexOffset = 0;
		}

		// Copy the animator component to the transform
		private void CopyAnimator(Animator anim, GameObject target) {
			if (target.GetComponentsInChildren<Animator> ().Length == 0) {
				Animator newAnimator = target.AddComponent (typeof(Animator)) as Animator;
				if (newAnimator != null) {
					newAnimator.applyRootMotion = anim.applyRootMotion;
					newAnimator.avatar = anim.avatar;
					newAnimator.updateMode = anim.updateMode;
					newAnimator.cullingMode = anim.cullingMode;
					newAnimator.runtimeAnimatorController = anim.runtimeAnimatorController;
				}
			}
		}

		// Find the transform in which to instanciate the animator component
		private Transform FindTransformForAnimator (Transform[] children, Transform[] rootBones, Animator anim) {
			foreach (Transform t in children) {
				for (int i = 0; i < rootBones.Length; i++) {
					if (t.Equals (rootBones [i])) {
						return rootBones [i];
					}
				}
			}
			return null;
		}

		// Return a correspondancy map between original bones and the new bones
		private void GetOrignialToNewBonesCorrespondancy(Transform rootBone, Transform newRootBone, Dictionary<int, Transform> originToNewBoneMap) {
			Transform[] rootBoneTransforms = rootBone.GetComponentsInChildren<Transform> ();
			Transform[] newRootBoneTransforms = newRootBone.GetComponentsInChildren<Transform> ();
			// Get correspondancy between original bones and the new ones recently created
			for(int i=0; i<newRootBoneTransforms.Length; i++) {
				if (!originToNewBoneMap.ContainsKey (rootBoneTransforms [i].GetInstanceID ())) {
					originToNewBoneMap.Add (rootBoneTransforms [i].GetInstanceID (), newRootBoneTransforms [i]);
				} else {
					Debug.LogWarning ("[Super Combiner] Found duplicated root bone: " + rootBoneTransforms [i]);
				}
			}
		}

		// Find the list of root bone from a hierachy of bones
		private Transform[] FindRootBone(Dictionary<int, Transform> bones) {
			List<Transform> rootBones = new List<Transform> ();
			List<Transform> bonesList = new List<Transform> (bones.Values);
			if (bonesList.Count == 0) {
				return rootBones.ToArray();
			}
			Transform rootBone = bonesList.ToArray () [0];
			while(rootBone.parent != null) {
				if (bones.ContainsKey (rootBone.parent.GetInstanceID())) {
					rootBone = rootBone.parent;
				} else {
					rootBones.Add(rootBone.parent);
					Transform[] children = rootBone.parent.GetComponentsInChildren<Transform> ();
					foreach (Transform t in children) {
						bones.Remove (t.GetInstanceID());
						if (t != rootBone.parent && rootBones.Contains (t)) {
							rootBones.Remove (t);
						}
					}
					Transform[] otherBones = (new List<Transform> (bones.Values)).ToArray ();
					if (otherBones.Length > 0) {
						rootBone = otherBones [0];
					} else {
						break;
					}
				}
			}
			return rootBones.ToArray();
		}

		// Instantiate a copy of the GameObject, keeping it's transform values identical
		private GameObject InstantiateCopy(GameObject original) {
			GameObject copy = GameObject.Instantiate(original) as GameObject;
			copy.transform.parent = original.transform.parent;
			copy.transform.localPosition = original.transform.localPosition;
			copy.transform.localRotation = original.transform.localRotation;
			copy.transform.localScale = original.transform.localScale;
			copy.name = original.name;

			// Remove all SkinnedMeshRenderes that may be inside root hierarchy
			foreach (SkinnedMeshRenderer skin in copy.GetComponentsInChildren<SkinnedMeshRenderer>()) {
				GameObject.DestroyImmediate (skin);
			}

			return copy;
		}

		// Create a new combineInstance based on a new mesh
		private CombineInstanceID createCombinedInstances(Mesh mesh, Material[] sharedMaterials, int instanceID, string name, Matrix4x4 matrix)  {
			CombineInstanceID instances = new CombineInstanceID ();
			int[] textureIndexes = new int[mesh.subMeshCount];
			for (int k = 0; k < mesh.subMeshCount; k++) {
				// Find corresponding material
				Material mat = sharedMaterials[k];
				textureIndexes[k] = combinedResult.FindCorrespondingMaterialIndex(mat);
			}

			// Update submesh count
			combinedResult.subMeshCount += mesh.subMeshCount;

            // Generate new UVs
			GenerateUV(mesh, textureIndexes, combinedResult.scaleFactors.ToArray(), name);

			for (int k = 0; k < mesh.subMeshCount; k++) {
				instances.AddCombineInstance(k, mesh, matrix, instanceID, name);
			}

			return instances;
		}

		// Create a new GameObject based on the CombineInstance list
		private GameObject createCombinedSkinnedMeshGameObject(CombineInstanceID instances, Transform parent, int number) {
			GameObject combined = new GameObject (sessionName + number.ToString());
			SkinnedMeshRenderer skinnedMeshRenderer = combined.AddComponent<SkinnedMeshRenderer>();
            
			skinnedMeshRenderer.sharedMaterial = combinedResult.material;
			skinnedMeshRenderer.sharedMesh = new Mesh ();	
			skinnedMeshRenderer.sharedMesh.name = sessionName + "_mesh" + number;
			skinnedMeshRenderer.sharedMesh.CombineMeshes (instances.combineInstances.ToArray(), true, true);

			#if UNITY_5_3_OR_NEWER
			// Add blendShapes to new skinnedMesh renderer if needed
			foreach (BlendShapeFrame blendShape in blendShapes.Values) {
				Vector3[] detlaVertices = new Vector3[skinnedMeshRenderer.sharedMesh.vertexCount];
				Vector3[] detlaNormals = new Vector3[skinnedMeshRenderer.sharedMesh.vertexCount];
				Vector3[] detlaTangents = new Vector3[skinnedMeshRenderer.sharedMesh.vertexCount];

				for (int p = 0; p < blendShape.deltaVertices.Length; p++) {
					detlaVertices.SetValue (blendShape.deltaVertices[p], p + blendShape.vertexOffset);
					detlaNormals.SetValue (blendShape.deltaNormals[p], p + blendShape.vertexOffset);
					detlaTangents.SetValue (blendShape.deltaTangents[p], p + blendShape.vertexOffset);
				}

				skinnedMeshRenderer.sharedMesh.AddBlendShapeFrame (blendShape.shapeName, blendShape.frameWeight, detlaVertices, detlaNormals, detlaTangents);
			}
			#endif

			#if UNITY_EDITOR
			MeshUtility.Optimize (skinnedMeshRenderer.sharedMesh);
			#endif
			combined.transform.SetParent(parent);
			combined.transform.localPosition = Vector3.zero;

            combinedResult.totalVertexCount += skinnedMeshRenderer.sharedMesh.vertexCount;
            combinedResult.AddCombinedMesh(skinnedMeshRenderer.sharedMesh, instances);
            return combined;
		}

        /// <summary>
        /// Create a new GameObject based on the CombineInstance list.
        /// Set its MeshFilter and MeshRenderer to the new combined Meshe/Material
        /// </summary>
        /// <param name="instances"></param>
        /// <param name="parent"></param>
        /// <param name="number"></param>
        /// <returns></returns>
		public GameObject CreateCombinedMeshGameObject(CombineInstanceID instances, Transform parent, int number) {
            GameObject combined;
            MeshFilter meshFilter;
            MeshRenderer meshRenderer;

            // If parent has components MeshFilters and MeshRenderers, replace meshes and materials
            if (number == 0 && parent.GetComponent<MeshFilter>() != null && parent.GetComponent<MeshRenderer>() != null)
            {
			    combined = parent.gameObject;
			    meshFilter = parent.GetComponent<MeshFilter>();
                meshRenderer = parent.GetComponent<MeshRenderer>();
            } else
            {
                combined = new GameObject(sessionName + number.ToString());
                meshFilter = combined.AddComponent<MeshFilter>();
                meshRenderer = combined.AddComponent<MeshRenderer>();
				combined.transform.SetParent(parent);
				combined.transform.localPosition = Vector3.zero;
            }

			meshRenderer.sharedMaterial = combinedResult.material;
			meshFilter.mesh = new Mesh ();
			meshFilter.sharedMesh.name = sessionName + "_mesh" + number;
			meshFilter.sharedMesh.CombineMeshes (instances.combineInstances.ToArray());
#if UNITY_EDITOR
			MeshUtility.Optimize (meshFilter.sharedMesh);
            Unwrapping.GenerateSecondaryUVSet(meshFilter.sharedMesh);
#endif            

            combinedResult.totalVertexCount += meshFilter.sharedMesh.vertexCount;
            combinedResult.AddCombinedMesh(meshFilter.sharedMesh, instances);
			return combined;
		}

		// Generate the new transformed gameobjects and apply new materials to them
		public bool GenerateUV(Mesh targetMesh, int[] textureIndex, float[] scaleFactors, string objectName)
		{		
			int subMeshCount = targetMesh.subMeshCount;
			//Debug.Log("[Super Combiner] Processing '" + objectName + "'...");

			Vector2[] uv = (Vector2[])(targetMesh.uv);
			Vector2[] uv2 = (Vector2[])(targetMesh.uv2);
			Vector2[] new_uv = new Vector2[uv.Length];
			Vector2[] new_uv2 = new Vector2[uv2.Length];
			Rect[] uvsInAtlasTexture = new Rect[subMeshCount];

			if (new_uv.Length > 0) {
				for (int i = 0; i < subMeshCount; i++) {
					// Get the list of triangles for the current submesh
					int[] subMeshTriangles = targetMesh.GetTriangles (i);
                    
                    if (textureIndex[i] < combinedResult.uvs.Length) {
                        uvsInAtlasTexture[i] = combinedResult.uvs[textureIndex[i]];

                        // Target UV calculation, taking into account main map's scale and offset of the original material
                        Rect targetUV = new Rect(uvsInAtlasTexture[i].position, uvsInAtlasTexture[i].size);

						float factor = scaleFactors[textureIndex[i]];
                        if (factor > 1) {
                            targetUV.size = Vector2.Scale(targetUV.size, Vector2.one / factor);
                            targetUV.position += new Vector2(uvsInAtlasTexture[i].width * (1 - 1/factor) / 2f, uvsInAtlasTexture[i].height * (1 - 1/factor) / 2f);
                        }

                        for (int j = 0; j < subMeshTriangles.Length; j++) {
							int uvIndex = subMeshTriangles [j];

							new_uv [uvIndex] = uv [uvIndex];

							// Translate new mesh's uvs so that minimun is at coordinates (0, 0)
							new_uv [uvIndex].x -= meshUVBounds [textureIndex [i]].xMin;
							new_uv [uvIndex].y -= meshUVBounds [textureIndex [i]].yMin;

							// Scale (if necessary) new mesh's uvs so that it fits in a (1, 1) square
							if (meshUVBounds [textureIndex [i]].width != 0 && meshUVBounds [textureIndex [i]].width != 1) {
								new_uv [uvIndex].Scale (new Vector2 (1 / meshUVBounds [textureIndex [i]].width, 1));
							}
							if (meshUVBounds [textureIndex [i]].height != 0 && meshUVBounds [textureIndex [i]].height != 1) {
								new_uv [uvIndex].Scale (new Vector2 (1, 1 / meshUVBounds [textureIndex [i]].height));
							}

							// Scale and translate new uvs to fit the correct texture in the atlas
							new_uv [uvIndex].Scale (targetUV.size);
							new_uv [uvIndex] += targetUV.position;
						}
					} else {
						Debug.LogError ("[Super Combiner] Texture index exceed packed texture size");
					}
				}
			} else {
				Debug.LogWarning ("[Super Combiner] Object " + objectName + " doesn't have uv, combine process may be incorrect. Add uv map with a 3d modeler tool.");
			}

			// Assign new uv
			targetMesh.uv = new_uv;

			// Lightmap
			if (uv2 != null && uv2.Length > 0 && combinedResult.uvs2 != null && combinedResult.uvs2.Length > 0) {
				for (int l = 0; l < uv2.Length; l++) {
					new_uv2 [uv2.Length + l] = new Vector2((uv2[l].x * combinedResult.uvs2[textureIndex[0]].width) + combinedResult.uvs2[textureIndex[0]].x, (uv2[l].y * combinedResult.uvs2[textureIndex[0]].height) + combinedResult.uvs2[textureIndex[0]].y);
				}
				targetMesh.uv2 = new_uv2;
			} else {
				// target mesh doesn't have uv2
			}
			return true;
		}

		// Copy a Mesh into a new instance
		public Mesh copyMesh(Mesh mesh, string id = "") {
			Mesh copy = new Mesh();
			copy.vertices = mesh.vertices;
			copy.normals = mesh.normals;
			copy.uv = mesh.uv;
			copy.uv2 = mesh.uv2;
			copy.uv3 = mesh.uv3;
			copy.uv4 = mesh.uv4;
			copy.triangles = mesh.triangles;
			copy.tangents = mesh.tangents;
			copy.subMeshCount = mesh.subMeshCount;
			copy.bindposes = mesh.bindposes;
			copy.boneWeights = mesh.boneWeights;
			copy.bounds = mesh.bounds;
			copy.colors32 = mesh.colors32;
			copy.name = mesh.name;

			copy.RecalculateBounds ();
			copy.RecalculateNormals ();

			for (int i = 0; i < mesh.subMeshCount; i++) {
				copy.SetIndices (mesh.GetIndices (i), mesh.GetTopology(i), i);
			}

			#if UNITY_5_3_OR_NEWER
			// Blendshape management
			if (mesh.blendShapeCount > 0) {
				Vector3[] deltaVertices = new Vector3[mesh.vertexCount];
				Vector3[] deltaNormals = new Vector3[mesh.vertexCount];
				Vector3[] deltaTangents = new Vector3[mesh.vertexCount];
				for (int s = 0; s < mesh.blendShapeCount; s++) {
					for (int f = 0; f < mesh.GetBlendShapeFrameCount (s); f++) {
						if(!blendShapes.ContainsKey(mesh.GetBlendShapeName (s)+id)) {
							// Copy blendShape to the new mesh
							mesh.GetBlendShapeFrameVertices (s, f, deltaVertices, deltaNormals, deltaTangents);
							copy.AddBlendShapeFrame (
								mesh.GetBlendShapeName (s), 
								mesh.GetBlendShapeFrameWeight (s, f),
								deltaVertices, deltaNormals, deltaTangents
							);
							// Add this blendShape to the list
							blendShapes.Add(mesh.GetBlendShapeName (s) + id, new BlendShapeFrame(mesh.GetBlendShapeName (s) + id, mesh.GetBlendShapeFrameWeight (s, f), deltaVertices, deltaNormals, deltaTangents, vertexOffset));
						}
					}
				}
			}
			#endif
			return copy;
		}

		// Copy a new mesh and assign it to destination
		private void CopyNewMeshesByCombine(Mesh original, Mesh destination) {
			int subMeshCount = original.subMeshCount;
			CombineInstance[] combineInstances = new CombineInstance[subMeshCount];
			for (int j = 0; j < subMeshCount; j++) {
				combineInstances [j] = new CombineInstance ();
				combineInstances [j].subMeshIndex = j;
				combineInstances [j].mesh = original;
				combineInstances [j].transform = Matrix4x4.identity;
			}

			destination.CombineMeshes (combineInstances, false);
		}
	}
}