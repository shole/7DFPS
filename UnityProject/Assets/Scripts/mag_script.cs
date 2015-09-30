using UnityEngine;
using System.Collections;

public class mag_script : MonoBehaviour {
	private int num_rounds= 8;
    [UnityEngine.Serialization.FormerlySerializedAs("kMaxRounds")]
	public int max_rounds= 8;
	private Vector3[] round_pos;
	private Quaternion[] round_rot;
	private Vector3 old_pos;
	public Vector3 hold_offset;
	public Vector3 hold_rotation;
	public bool collided= false;
	public AudioClip[] sound_add_round;
	public AudioClip[] sound_mag_bounce;
	public float life_time= 0.0f;
	public enum MagLoadStage {NONE, PUSHING_DOWN, ADDING_ROUND, REMOVING_ROUND, PUSHING_UP};
	public MagLoadStage mag_load_stage= MagLoadStage.NONE;
	public float mag_load_progress= 0.0f;
	public bool disable_interp= true;

	private Rigidbody _rigidbody;
	private Collider _collider;

	public bool RemoveRound (){
		 if(num_rounds == 0){
			return false;
		}
		var round_obj= transform.FindChild("round_"+num_rounds);
		round_obj.GetComponent<Renderer>().enabled = false;
		--num_rounds;
		return true;
	}

	public bool RemoveRoundAnimated (){
		 if(num_rounds == 0 || mag_load_stage != MagLoadStage.NONE){
			return false;
		}
		mag_load_stage = MagLoadStage.REMOVING_ROUND;
		mag_load_progress = 0.0f;
		return true;
	}

	public bool IsFull (){
		 return num_rounds == max_rounds;
	}

	public bool AddRound (){
		 if(num_rounds >= max_rounds || mag_load_stage != MagLoadStage.NONE){
			return false;
		}
		mag_load_stage = MagLoadStage.PUSHING_DOWN;
		mag_load_progress = 0.0f;
		PlaySoundFromGroup(sound_add_round, 0.3f);
		++num_rounds;
		var round_obj= transform.FindChild("round_"+num_rounds);
		round_obj.GetComponent<Renderer>().enabled = true;
		return true;
	}

	public int NumRounds (){
		return num_rounds;
	}

	void  Start (){
		old_pos = transform.position;
		num_rounds = Random.Range(0,max_rounds);
		round_pos = new Vector3[max_rounds];
		round_rot = new Quaternion[max_rounds];
		for(var i=0; i<max_rounds; ++i){
			var round= transform.FindChild("round_"+(i+1));
			round_pos[i] = round.localPosition;
			round_rot[i] = round.localRotation;
			if(i < num_rounds){
				round.GetComponent<Renderer>().enabled = true;
			} else {
				round.GetComponent<Renderer>().enabled = false;
			}
		}
		_rigidbody = GetComponent<Rigidbody>();
		_collider = GetComponent<Collider>();
	}

	void  PlaySoundFromGroup ( AudioClip[] group ,   float volume  ){
		if(group.Length == 0){return;}
		var which_shot= Random.Range(0,group.Length);
		GetComponent<AudioSource>().PlayOneShot(group[which_shot], volume * PlayerPrefs.GetFloat("sound_volume", 1.0f));
	}

	void  CollisionSound (){
		if(!collided){
			collided = true;
			PlaySoundFromGroup(sound_mag_bounce, 0.3f);
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
		} else {
			life_time = 0.0f;
			collided = false;
		}
		old_pos = transform.position;
	}

	void  Update (){
		switch(mag_load_stage){
			case MagLoadStage.PUSHING_DOWN:
				mag_load_progress += Time.deltaTime * 20.0f;
				if(mag_load_progress >= 1.0f){
					mag_load_stage = MagLoadStage.ADDING_ROUND;
					mag_load_progress = 0.0f;
				}
				break;
			case MagLoadStage.ADDING_ROUND:
				mag_load_progress += Time.deltaTime * 20.0f;
				if(mag_load_progress >= 1.0f){
					mag_load_stage = MagLoadStage.NONE;
					mag_load_progress = 0.0f;
					for(var i=0; i<num_rounds; ++i){
						var obj= transform.FindChild("round_"+(i+1));
						obj.localPosition = round_pos[i];
						obj.localRotation = round_rot[i];
					}
				}
				break;
			case MagLoadStage.PUSHING_UP:
				mag_load_progress += Time.deltaTime * 20.0f;
				if(mag_load_progress >= 1.0f){
					mag_load_stage = MagLoadStage.NONE;
					mag_load_progress = 0.0f;
					RemoveRound();
					for(var i=0; i<num_rounds; ++i){
						var obj = transform.FindChild("round_"+(i+1));
						obj.localPosition = round_pos[i];
						obj.localRotation = round_rot[i];
					}
				}
				break;
			case MagLoadStage.REMOVING_ROUND:
				mag_load_progress += Time.deltaTime * 20.0f;
				if(mag_load_progress >= 1.0f){
					mag_load_stage = MagLoadStage.PUSHING_UP;
					mag_load_progress = 0.0f;
				}
				break;
		}
		var mag_load_progress_display= mag_load_progress;
		if(disable_interp){
			mag_load_progress_display = Mathf.Floor(mag_load_progress + 0.5f);
		}
		switch(mag_load_stage){
			case MagLoadStage.PUSHING_DOWN:
				var obj = transform.FindChild("round_1");
				obj.localPosition = Vector3.Lerp(transform.FindChild("point_start_load").localPosition, 
												 transform.FindChild("point_load").localPosition, 
												 mag_load_progress_display);
				obj.localRotation = Quaternion.Slerp(transform.FindChild("point_start_load").localRotation, 
													 transform.FindChild("point_load").localRotation, 
													 mag_load_progress_display);
				for(var i=1; i<num_rounds; ++i){
					obj = transform.FindChild("round_"+(i+1));
					obj.localPosition = Vector3.Lerp(round_pos[i-1], round_pos[i], mag_load_progress_display);
					obj.localRotation = Quaternion.Slerp(round_rot[i-1], round_rot[i], mag_load_progress_display);
				}
				break;
			case MagLoadStage.ADDING_ROUND:
				obj = transform.FindChild("round_1");
				obj.localPosition = Vector3.Lerp(transform.FindChild("point_load").localPosition, 
												 round_pos[0], 
												 mag_load_progress_display);
				obj.localRotation = Quaternion.Slerp(transform.FindChild("point_load").localRotation, 
													 round_rot[0], 
													 mag_load_progress_display);
				for(var i=1; i<num_rounds; ++i){
					obj = transform.FindChild("round_"+(i+1));
					obj.localPosition = round_pos[i];
				}
				break;
			case MagLoadStage.PUSHING_UP:
				obj = transform.FindChild("round_1");
				obj.localPosition = Vector3.Lerp(transform.FindChild("point_start_load").localPosition, 
												 transform.FindChild("point_load").localPosition, 
												 1.0f-mag_load_progress_display);
				obj.localRotation = Quaternion.Slerp(transform.FindChild("point_start_load").localRotation, 
													 transform.FindChild("point_load").localRotation, 
													 1.0f-mag_load_progress_display);
				for(var i=1; i<num_rounds; ++i){
					obj = transform.FindChild("round_"+(i+1));
					obj.localPosition = Vector3.Lerp(round_pos[i-1], round_pos[i], mag_load_progress_display);
					obj.localRotation = Quaternion.Slerp(round_rot[i-1], round_rot[i], mag_load_progress_display);
				}
				break;
			case MagLoadStage.REMOVING_ROUND:
				obj = transform.FindChild("round_1");
				obj.localPosition = Vector3.Lerp(transform.FindChild("point_load").localPosition, 
												 round_pos[0], 
												 1.0f-mag_load_progress_display);
				obj.localRotation = Quaternion.Slerp(transform.FindChild("point_load").localRotation, 
													 round_rot[0], 
													 1.0f-mag_load_progress_display);
				for(var i=1; i<num_rounds; ++i){
					obj = transform.FindChild("round_"+(i+1));
					obj.localPosition = round_pos[i];
					obj.localRotation = round_rot[i];
				}
				break;
		}
	}

	void  OnCollisionEnter ( Collision collision  ){
		CollisionSound();
	}
}