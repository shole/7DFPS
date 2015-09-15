using UnityEngine;
using System.Collections;

public class BulletPileScript : MonoBehaviour {
	void  Start (){
		var holder= GameObject.Find("gui_skin_holder").GetComponent<GUISkinHolder>();
		var weapon_holder= holder.weapon.GetComponent<WeaponHolder>();
		var num_bullets= Random.Range(1,6);
		for(var i=0; i<num_bullets; ++i){
			GameObject bullet = Instantiate(weapon_holder.bullet_object);
			bullet.transform.position = transform.position + 
				new Vector3(Random.Range(-0.1f,0.1f),
						Random.Range(0.0f,0.2f),
						Random.Range(-0.1f,0.1f));
			bullet.transform.rotation = BulletScript.RandomOrientation();
			bullet.AddComponent<Rigidbody>();
			bullet.GetComponent<ShellCasingScript>().collided = true;
		}
		if(Random.Range(0,4) == 0){
			GameObject tape = Instantiate(holder.tape_object);
			tape.transform.position = transform.position + 
				new Vector3(Random.Range(-0.1f,0.1f),
						Random.Range(0.0f,0.2f),
						Random.Range(-0.1f,0.1f));
			tape.transform.rotation = BulletScript.RandomOrientation();		
		}
		if(Random.Range(0,4) == 0 && !holder.has_flashlight){
			GameObject flashlight = Instantiate(holder.flashlight_object);
			flashlight.transform.position = transform.position + 
				new Vector3(Random.Range(-0.1f,0.1f),
						Random.Range(0.2f,0.4f),
						Random.Range(-0.1f,0.1f));
			flashlight.transform.rotation = BulletScript.RandomOrientation();
		}
	}
}