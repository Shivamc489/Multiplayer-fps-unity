using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class describing data for a unique blendshape
/// </summary>
namespace LunarCatsStudio.SuperCombiner {
	public class BlendShapeFrame {
		public string shapeName;
		public float frameWeight;
		public Vector3[] deltaVertices;
		public Vector3[] deltaNormals;
		public Vector3[] deltaTangents;
		public int vertexOffset;

		public BlendShapeFrame(string shapeName_p, float frameWeight_p, Vector3[] deltaVertices_p, Vector3[] deltaNormals_p, Vector3[] deltaTangents_p, int offset) {
			shapeName = shapeName_p;
			frameWeight = frameWeight_p;
			deltaVertices = deltaVertices_p;
			deltaNormals = deltaNormals_p;
			deltaTangents = deltaTangents_p;
			vertexOffset = offset;
		}
	}
}
