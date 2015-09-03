#pragma strict

private var life_time = 0.0;
private var old_pos : Vector3;

function Start () {
	old_pos = transform.position;
}

function Update () {
	transform.FindChild("light_obj").GetComponent.<Light>().intensity = 1.0 + Mathf.Sin(Time.time * 2.0);
}

function FixedUpdate() {
	if(GetComponent.<Rigidbody>() && !GetComponent.<Rigidbody>().IsSleeping() && GetComponent.<Collider>() && GetComponent.<Collider>().enabled){
		life_time += Time.deltaTime;
		var hit : RaycastHit;
		if(Physics.Linecast(old_pos, transform.position, hit, 1)){
			transform.position = hit.point;
			transform.GetComponent.<Rigidbody>().velocity *= -0.3;
		}
		if(life_time > 2.0){
			GetComponent.<Rigidbody>().Sleep();
		}
	}
	old_pos = transform.position;
}