using UnityEngine;
using System.Collections;

public class BulletScript : MonoBehaviour {
	public AudioClip[] sound_hit_concrete;
	public AudioClip[] sound_hit_metal;
	public AudioClip[] sound_hit_glass;
	public AudioClip[] sound_hit_body;
	public AudioClip[] sound_hit_ricochet;
	public AudioClip[] sound_glass_break;
	public AudioClip[] sound_flyby;
	public GameObject bullet_obj;
	public GameObject bullet_hole_obj;
	public GameObject glass_bullet_hole_obj;
	public GameObject metal_bullet_hole_obj;
	public GameObject spark_effect;
	public GameObject puff_effect;
	private Vector3 old_pos;
	private bool hit_something= false;
	private LineRenderer line_renderer; 
	private Vector3 velocity;
	private float life_time= 0.0f;
	private float death_time= 0.0f;
	private int segment= 1;
	private bool hostile= false;

	public void  SetVelocity ( Vector3 vel  ){
		this.velocity = vel;
	}

	public void  SetHostile (){
		GetComponent<AudioSource>().rolloffMode = AudioRolloffMode.Logarithmic;
		PlaySoundFromGroup(sound_flyby, 0.4f);
		hostile = true;
	}
			
	void  Start (){
		line_renderer = GetComponent<LineRenderer>();
		line_renderer.SetPosition(0, transform.position);
		line_renderer.SetPosition(1, transform.position);
		old_pos = transform.position;
	}

	T RecursiveHasScript<T>( GameObject obj,  int depth  )  where T : MonoBehaviour {
		if(obj.GetComponent<T>()){
			return obj.GetComponent<T>();
		} else if(depth > 0 && obj.transform.parent){
			return RecursiveHasScript<T>(obj.transform.parent.gameObject, depth-1);
		} else {
			return null;
		}
	}

	public static Quaternion RandomOrientation (){
		return Quaternion.Euler(Random.Range(0,360),Random.Range(0,360),Random.Range(0,360));
	}

	void  PlaySoundFromGroup ( AudioClip[] group ,   float volume  ){
		var which_shot= Random.Range(0,group.Length);
		GetComponent<AudioSource>().PlayOneShot(group[which_shot], volume * PlayerPrefs.GetFloat("sound_volume", 1.0f));
	}

	void  Update (){
		if(!hit_something){
			life_time += Time.deltaTime;
			if(life_time > 1.5f){
				hit_something = true;
			}
			transform.position += velocity * Time.deltaTime;
			velocity += Physics.gravity * Time.deltaTime;
			RaycastHit hit;
			if(Physics.Linecast(old_pos, transform.position, out hit, 1<<0 | 1<<9 | 1<<11)){
				var hit_obj= hit.collider.gameObject;
				var hit_transform_obj= hit.transform.gameObject;
				ShootableLight light_script = RecursiveHasScript<ShootableLight>(hit_obj, 1);
				AimScript aim_script = RecursiveHasScript<AimScript>(hit_obj, 1);
				RobotScript turret_script = RecursiveHasScript<RobotScript>(hit_obj, 3);
				transform.position = hit.point;
				var ricochet_amount= Vector3.Dot(velocity.normalized, hit.normal) * -1.0f;
				if(Random.Range(0.0f,1.0f) > ricochet_amount && Vector3.Magnitude(velocity) * (1.0f-ricochet_amount) > 10.0f){
					var ricochet= Instantiate(bullet_obj, hit.point, transform.rotation) as GameObject;
					var ricochet_vel= velocity * 0.3f * (1.0f-ricochet_amount);
					velocity -= ricochet_vel;
					ricochet_vel = Vector3.Reflect(ricochet_vel, hit.normal);
					ricochet.GetComponent<BulletScript>().SetVelocity(ricochet_vel);
					PlaySoundFromGroup(sound_hit_ricochet, hostile ? 1.0f : 0.6f);
				} else if(turret_script && velocity.magnitude > 100.0f){
					RaycastHit new_hit;
					if(Physics.Linecast(hit.point + velocity.normalized * 0.001f, hit.point + velocity.normalized, out new_hit, 1<<11 | 1<<12)){
						if(new_hit.collider.gameObject.layer == 12){
							turret_script.WasShotInternal(new_hit.collider.gameObject);
						}
					}					
				}
				if(hit_transform_obj.GetComponent<Rigidbody>()){
					hit_transform_obj.GetComponent<Rigidbody>().AddForceAtPosition(velocity * 0.01f, hit.point, ForceMode.Impulse);
				}
				if(light_script){
					light_script.WasShot(hit_obj, hit.point, velocity);
					if(hit.collider.material.name == "glass (Instance)"){
						PlaySoundFromGroup(sound_glass_break, 1.0f);
					}
				}
				if(Vector3.Magnitude(velocity) > 50){
					GameObject hole;
					GameObject effect;
					if(turret_script){
						PlaySoundFromGroup(sound_hit_metal, hostile ? 1.0f : 0.8f);
						hole = Instantiate(metal_bullet_hole_obj, hit.point, RandomOrientation()) as GameObject;
						effect = Instantiate(spark_effect, hit.point, RandomOrientation()) as GameObject;
						turret_script.WasShot(hit_obj, hit.point, velocity);
					} else if(aim_script){
						hole = Instantiate(bullet_hole_obj, hit.point, RandomOrientation()) as GameObject;
						effect = Instantiate(puff_effect, hit.point, RandomOrientation()) as GameObject;
						PlaySoundFromGroup(sound_hit_body, 1.0f);
						aim_script.WasShot();
					} else if(hit.collider.material.name == "metal (Instance)"){
						PlaySoundFromGroup(sound_hit_metal, hostile ? 1.0f : 0.4f);
						hole = Instantiate(metal_bullet_hole_obj, hit.point, RandomOrientation()) as GameObject;
						effect = Instantiate(spark_effect, hit.point, RandomOrientation()) as GameObject;
					} else if(hit.collider.material.name == "glass (Instance)"){
						PlaySoundFromGroup(sound_hit_glass, hostile ? 1.0f : 0.4f);
						hole = Instantiate(glass_bullet_hole_obj, hit.point, RandomOrientation()) as GameObject;
						effect = Instantiate(spark_effect, hit.point, RandomOrientation()) as GameObject;
					} else {
						PlaySoundFromGroup(sound_hit_concrete, hostile ? 1.0f : 0.4f);
						hole = Instantiate(bullet_hole_obj, hit.point, RandomOrientation()) as GameObject;
						effect = Instantiate(puff_effect, hit.point, RandomOrientation()) as GameObject;
					}
					effect.transform.position += hit.normal * 0.05f;
					hole.transform.position += hit.normal * 0.01f;
					if(!aim_script){
						hole.transform.parent = hit_obj.transform;
					} else {
						hole.transform.parent = GameObject.Find("Main Camera").transform;
					}
				}
				hit_something = true;
			}
			line_renderer.SetVertexCount(segment+1);
			line_renderer.SetPosition(segment, transform.position);
			++segment;
		} else {
			life_time += Time.deltaTime;
			death_time += Time.deltaTime;
			//Destroy(this.gameObject);
		}
		for(var i=0; i<segment; ++i){
			var start_color= new Color(1,1,1,(1.0f - life_time * 5.0f)*0.05f);
			var end_color= new Color(1,1,1,(1.0f - death_time * 5.0f)*0.05f);
			line_renderer.SetColors(start_color, end_color);
			if(death_time > 1.0f){
				Destroy(this.gameObject);
			}
		}
	}
}