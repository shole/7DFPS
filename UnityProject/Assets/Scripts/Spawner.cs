#pragma strict

var target : GameObject;

public function Spawn(pos : Vector3, rot : Quaternion) : GameObject{
	var tr = transform;
	var instance = Instantiate(target, pos, rot);
	return instance;
}