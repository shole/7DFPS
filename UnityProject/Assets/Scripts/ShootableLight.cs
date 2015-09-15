using UnityEngine;
using System.Collections;

public class ShootableLight : MonoBehaviour {
	public GameObject destroy_effect;
	public Color light_color= new Color(1,1,1);
	public bool destroyed= false;
	public enum LightType {AIRPLANE_BLINK, NORMAL, FLICKER}
	public LightType light_type= LightType.NORMAL;
	private float blink_delay= 0.0f;

	private float light_amount= 1.0f;

	private Light[] lights;
	private MeshRenderer[] renderers;
	private bool[] rendererIsShade;
	private int illumProp;
	private float lastLightAmount= -1.0f;

	public void  WasShot ( GameObject obj ,   Vector3 pos ,   Vector3 vel  ){
		if(!destroyed){
			destroyed = true;
			light_amount = 0.0f;
			Instantiate(destroy_effect, transform.FindChild("bulb").position, Quaternion.identity);
		}
		if(obj && obj.GetComponent<Collider>() && obj.GetComponent<Collider>().material.name == "glass (Instance)"){
			GameObject.Destroy(obj);
		}
	}

	void  Start (){
		ResetCache();
		illumProp = Shader.PropertyToID("_Illum");
	}

	void  ResetCache (){
		lights = gameObject.GetComponentsInChildren<Light>();
		renderers = gameObject.GetComponentsInChildren<MeshRenderer>();
		rendererIsShade = new bool[ renderers.Length];
		for (var i= 0; i < renderers.Length; ++i)
		{
			rendererIsShade[i] = renderers[i].gameObject.name == "shade";
		}
	}

	void  Update (){
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

		var combined_color= new Color(light_color.r * light_amount,light_color.g * light_amount,light_color.b * light_amount);
		var i= 0;
		var cacheDirty= false;
		for (; i < lights.Length; ++i)
		{
			if (lights[i] == null){
				cacheDirty = true;
				continue;
			}
			lights[i].color = combined_color;
		}
		for (i = 0; i < renderers.Length; ++i)
		{
			if (renderers[i] == null){
				cacheDirty = true;
				continue;
			}
			var renderer= renderers[i];
			var mat= renderer.material;
			mat.SetColor(illumProp, combined_color * (rendererIsShade[i] ? 0.5f : 1f));
		}
		if (cacheDirty)
		{
			ResetCache();
		}
	}
}