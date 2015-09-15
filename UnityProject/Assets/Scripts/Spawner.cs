using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour {
	public GameObject target;

	public GameObject Spawn ( Vector3 pos ,   Quaternion rot  ) {
		var instance = Instantiate(target, pos, rot) as GameObject;
		return instance;
	}
}