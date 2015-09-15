using UnityEngine;
using System.Collections;

public class ShellCasingScript : MonoBehaviour {
	public AudioClip[] sound_shell_bounce;
	public bool collided= false;
	public Vector3 old_pos;
	public float life_time= 0.0f;
	public float glint_delay= 0.0f;
	public float glint_progress= 0.0f;
	private Light glint_light;

	private Rigidbody _rigidbody;
	private Collider _collider;

	void  PlaySoundFromGroup ( AudioClip[] group ,   float volume  ){
		var which_shot= Random.Range(0,group.Length);
		GetComponent<AudioSource>().PlayOneShot(group[which_shot], volume * PlayerPrefs.GetFloat("sound_volume", 1.0f));
	}

	void  Start (){
		_rigidbody = GetComponent<Rigidbody>();
		_collider = GetComponent<Collider>();
		old_pos = transform.position;
		if(transform.FindChild("light_pos")){
			glint_light = transform.FindChild("light_pos").GetComponent<Light>();
			glint_light.enabled = false;
		}
	}

	void  CollisionSound (){
		if(!collided){
			collided = true;
			PlaySoundFromGroup(sound_shell_bounce, 0.3f);
		}
	}

	void  FixedUpdate (){
		if (_rigidbody)
		{
			if(!_rigidbody.IsSleeping() && _collider && _collider.enabled){
				life_time += Time.fixedDeltaTime;
				RaycastHit hit;
				if(Physics.Linecast(old_pos, transform.position, out hit, 1)){
					transform.position = hit.point;
					_rigidbody.velocity *= -0.3f;
				}
				if(life_time > 2.0f){
					_rigidbody.Sleep();
				}
			}
			if(_rigidbody.IsSleeping() && glint_light){
				if(glint_delay == 0.0f){
					glint_delay = Random.Range(1.0f,5.0f);
				}
				glint_delay = Mathf.Max(0.0f, glint_delay - Time.deltaTime);
				if(glint_delay == 0.0f){
					glint_progress = 1.0f;
				}
				if(glint_progress > 0.0f){
					glint_light.enabled = true;
					glint_light.intensity = Mathf.Sin(glint_progress * Mathf.PI);
					glint_progress = Mathf.Max(0.0f, glint_progress - Time.deltaTime * 2.0f);
				} else {
					glint_light.enabled = false;
				}
			}
		}
		old_pos = transform.position;
	}

	void  OnCollisionEnter ( Collision collision  ){
		CollisionSound();
	}
}