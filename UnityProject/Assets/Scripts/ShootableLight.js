#pragma strict

var destroy_effect : GameObject;
var light_color = Color(1,1,1);
var destroyed = false;
enum LightType {AIRPLANE_BLINK, NORMAL, FLICKER}
public var light_type = LightType.NORMAL;
private var blink_delay = 0.0f;

private var light_amount = 1.0f;

private var lights : Light[];
private var renderers : MeshRenderer[];
private var rendererIsShade : boolean[];
private var illumProp : int;
private var lastLightAmount = -1.0f;

function WasShot(obj : GameObject, pos : Vector3, vel : Vector3) {
	if(!destroyed){
		destroyed = true;
		light_amount = 0.0;
		Instantiate(destroy_effect, transform.FindChild("bulb").position, Quaternion.identity);
	}
	if(obj && obj.GetComponent.<Collider>() && obj.GetComponent.<Collider>().material.name == "glass (Instance)"){
		GameObject.Destroy(obj);
	}
}

function Start () {
	lights = gameObject.GetComponentsInChildren.<Light>();
	renderers = gameObject.GetComponentsInChildren.<MeshRenderer>();
	rendererIsShade = new boolean[renderers.Length];
	for (var i = 0; i < renderers.Length; ++i)
	{
		rendererIsShade[i] = renderers[i].gameObject.name == "shade";
	}
	illumProp = Shader.PropertyToID("_Illum");
}

function Update () {
	if(!destroyed){
		switch(light_type){
			case LightType.AIRPLANE_BLINK:
				if(blink_delay <= 0.0f){
					blink_delay = 1.0f;
					if(light_amount == 1.0f){
						light_amount = 0.0f;
					} else {
						light_amount = 1.0f;
					}
				}
				blink_delay -= Time.deltaTime;
				break;
		}
	}
	if (light_amount == lastLightAmount)
		return;
	lastLightAmount = light_amount;

	var combined_color = Color(light_color.r * light_amount,light_color.g * light_amount,light_color.b * light_amount);
	var i = 0;
	for (; i < lights.Length; ++i)
	{
		lights[i].color = combined_color;
	}
	for (i = 0; i < renderers.Length; ++i)
	{
		var renderer = renderers[i];
		var mat = renderer.material;
		mat.SetColor(illumProp, combined_color * (rendererIsShade[i] ? 0.5f : 1f));
	}
}