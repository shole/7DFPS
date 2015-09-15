using UnityEngine;
using System.Collections;

public class taser_spark : MonoBehaviour {
	public float opac = 0.0f;

	void  UpdateColor (){
		var renderers= transform.GetComponentsInChildren<MeshRenderer>();
		var color= new Vector4(opac,opac,opac,opac);
		foreach(MeshRenderer renderer in renderers){
			renderer.material.SetColor("_TintColor", color);
		}
		var lights= transform.GetComponentsInChildren<Light>();
		foreach(Light light in lights){
			light.intensity = opac * 2.0f;
		}
	}

	void  Start (){
		opac = Random.Range(0.4f,1.0f);
		UpdateColor();
		var localEuler = transform.localEulerAngles;
		localEuler.z = Random.Range (0.0f, 360.0f);
		transform.localEulerAngles = localEuler;
		transform.localScale = new Vector3(Random.Range(0.8f,2.0f), Random.Range(0.8f,2.0f), Random.Range(0.8f,2.0f));
	}

	void  Update (){
		UpdateColor();
		opac -= Time.deltaTime * 50.0f;
		if(opac <= 0.0f){
			Destroy(gameObject);
		}
	}
}