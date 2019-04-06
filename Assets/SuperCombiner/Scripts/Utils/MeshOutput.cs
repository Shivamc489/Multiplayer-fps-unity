using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
/// <summary>
/// Class describing the type of output when combining meshes
/// </summary>
namespace LunarCatsStudio.SuperCombiner {
	public enum MeshOutput {
		// Combine meshes/skinned meshes as a mesh
		Mesh = 0, 
		// Combine meshes/skinned meshes as a skinned mesh 
		SkinnedMesh = 1 
	};
}
