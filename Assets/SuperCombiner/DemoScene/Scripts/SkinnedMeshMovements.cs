using UnityEngine;
using System.Collections;

public class SkinnedMeshMovements : MonoBehaviour {

	public Transform[] waypoints;
	public float speed = 3.0f;
	private Rigidbody rigidBody;
	private int index;

	// Use this for initialization
	void Start () {
		rigidBody = GetComponent<Rigidbody> ();
		NewDestination ();
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 direction = speed * Vector3.Normalize (waypoints [index].position - transform.position);
		direction.y = 0;
		transform.rotation = Quaternion.LookRotation (direction);
		rigidBody.velocity = direction;

		if (direction.magnitude < 1) {
			NewDestination ();
		}
	}

	private void NewDestination() {
		index = Random.Range (0, waypoints.Length);
		rigidBody.velocity = Vector3.Normalize(waypoints[index].position - transform.position);
	}
}
