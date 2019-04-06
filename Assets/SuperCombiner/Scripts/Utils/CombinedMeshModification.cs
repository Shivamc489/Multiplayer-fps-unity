using UnityEngine;
using System.Collections;
using LunarCatsStudio.SuperCombiner;

namespace LunarCatsStudio.SuperCombiner
{
    /// <summary>
    /// Attach this script to each combined Gameobject that you wish to remove part during runtime.
    /// This only works for a combined GameObject with "combine mesh" parameter set to true.
    /// You can remove parts of the combined mesh using the "RemoveFromCombined" API. Use the instanceID of the object you wish to 
    /// remove. In order know the correct instanceID, check in the "combinedResults" file under "mesh Results" -> "Instance Ids".
    /// </summary>
    public class CombinedMeshModification : MonoBehaviour
    {
		// The combined result
		[Tooltip("Reference to the combinedResult file")]
        public CombinedResult combinedResult;
		// The MeshFilter to which the combinedMesh is set
        [Tooltip("Reference to the MeshFilter in which the combined mesh is attached to")]
		public MeshFilter meshFilter;

		// A new instance of combined result is created at runtime to keep original intact
		private CombinedResult currentCombinedResult;

        // Use this for initialization
        void Start()
        {
            // Instanciate a copy of the combinedResult
			currentCombinedResult = GameObject.Instantiate(combinedResult) as CombinedResult;
        }

        /// <summary>
        /// Remove a GameObject from the combined mesh
        /// </summary>
        /// <param name="gameObject"></param>
        public void RemoveFromCombined(GameObject gameObject)
        {
			RemoveFromCombined (gameObject.GetInstanceID ());
        }

        /// <summary>
        /// Remove a GameObject from the combined mesh
        /// </summary>
        /// <param name="instanceID"></param>
        public void RemoveFromCombined(int instanceID)
        {
			// Check if meshFilter is set
			if (meshFilter == null) 
			{
				Debug.LogWarning("[Super Combiner] MeshFilter is not set, please assign MeshFilter parameter before trying to remove a part of it's mesh");
				return;
			}
            bool success = false;
			foreach (MeshCombined meshResult in currentCombinedResult.meshResults)
            {
                if (meshResult.instanceIds.Contains(instanceID))
                {
                    Debug.Log("[Super Combiner] Removing object '" + instanceID + "' from combined mesh");
					meshFilter.mesh = meshResult.RemoveMesh(instanceID, meshFilter.mesh);
                    success = true;
                }
            }
            if (!success)
            {
				Debug.LogWarning("[Super Combiner] Could not remove object '" + instanceID + "' because it was not found");
            }
        }
    }

}