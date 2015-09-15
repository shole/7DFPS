using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

public class Spring {
	public float state;
	public float target_state;
	public float vel;
	public float strength;
	public float damping; 
	
	public Spring ( float state ,   float target_state ,   float strength ,   float damping  ){
		this.Set(state, target_state, strength, damping); 
	}
	public void  Set ( float state ,   float target_state ,   float strength ,   float damping  ){
		this.state = state;
		this.target_state = target_state;
		this.strength = strength;
		this.damping = damping;
		this.vel = 0.0f;		
	}
	public void  Update (){
		#if false //[DA] this was active, for some reason, turret behavior was really weird as were some other things
		this.state = Mathf.MoveTowards(this.state, this.target_state, this.strength * Time.deltaTime * 0.05f);		
		#else
		this.vel += (this.target_state - this.state) * this.strength * Time.deltaTime;
		this.vel *= Mathf.Pow(this.damping, Time.deltaTime);
		this.state += this.vel * Time.deltaTime;
		#endif
	}
};

public class AimScript : MonoBehaviour {
	// Prefabs 

	private GameObject magazine_obj;
	private GameObject gun_obj;
	private GameObject casing_with_bullet;
	public Texture texture_death_screen;

	public AudioClip[] sound_bullet_grab;
	public AudioClip[] sound_body_fall;
	public AudioClip[] sound_electrocute;

	public AudioSource audiosource_tape_background;
	public AudioSource audiosource_audio_content;

	// Shortcuts to components

	private GameObject main_camera;
	private CharacterController character_controller;
	private bool show_help= false;
	private bool show_advanced_help= false;
	private float help_hold_time= 0.0f;
	private bool help_ever_shown= false;
	private bool just_started_help= false;
				
	// Instances

	private GameObject gun_instance;

	// Public parameters

	private float sensitivity_x= 2.0f;
	private float sensitivity_y= 2.0f;
	private float min_angle_y= -89.0f;
	private float max_angle_y= 89.0f;

	private GUISkinHolder holder;
	private WeaponHolder weapon_holder;

	public bool disable_springs = false; 
	public bool disable_recoil = true;

	// Private variables
	private bool aim_toggle= false;
	private const float kAimSpringStrength = 100.0f;
	private const float kAimSpringDamping = 0.00001f;
	private Spring aim_spring= new Spring(0,0,kAimSpringStrength,kAimSpringDamping);

	private GameObject held_flashlight = null;
	private Vector3 flashlight_aim_pos;
	private Quaternion flashlight_aim_rot;
	private Spring flashlight_mouth_spring= new Spring(0,0,kAimSpringStrength,kAimSpringDamping);
	private Spring flash_ground_pose_spring= new Spring(0,0,kAimSpringStrength, kAimSpringDamping);
	private Vector3 flash_ground_pos;
	private Quaternion flash_ground_rot;

	private float rotation_x_leeway= 0.0f;
	private float rotation_y_min_leeway= 0.0f;
	private float rotation_y_max_leeway= 0.0f;
	private float kRotationXLeeway= 5.0f;
	private float kRotationYMinLeeway= 20.0f;
	private float kRotationYMaxLeeway= 10.0f;

	private float rotation_x= 0.0f;
	private float rotation_y= 0.0f;
	private float view_rotation_x= 0.0f;
	private float view_rotation_y= 0.0f;

	private const float kRecoilSpringStrength= 800.0f;
	private const float kRecoilSpringDamping= 0.000001f;
	private Spring x_recoil_spring= new Spring(0,0,kRecoilSpringStrength,kRecoilSpringDamping);
	private Spring y_recoil_spring= new Spring(0,0,kRecoilSpringStrength,kRecoilSpringDamping);
	private Spring head_recoil_spring_x= new Spring(0,0,kRecoilSpringStrength,kRecoilSpringDamping);
	private Spring head_recoil_spring_y= new Spring(0,0,kRecoilSpringStrength,kRecoilSpringDamping);

	private Vector3 mag_pos;
	private Quaternion mag_rot;

	private GameObject magazine_instance_in_hand;
	private float kGunDistance= 0.3f;

	private Spring slide_pose_spring= new Spring(0,0,kAimSpringStrength, kAimSpringDamping);
	private Spring reload_pose_spring= new Spring(0,0,kAimSpringStrength, kAimSpringDamping);
	private Spring press_check_pose_spring= new Spring(0,0,kAimSpringStrength, kAimSpringDamping);
	private Spring inspect_cylinder_pose_spring= new Spring(0,0,kAimSpringStrength, kAimSpringDamping);
	private Spring add_rounds_pose_spring= new Spring(0,0,kAimSpringStrength, kAimSpringDamping);
	private Spring eject_rounds_pose_spring= new Spring(0,0,kAimSpringStrength, kAimSpringDamping);

	enum GunTilt {LEFT, CENTER, RIGHT};
	private GunTilt gun_tilt = GunTilt.CENTER;

	private Spring hold_pose_spring= new Spring(0,0,kAimSpringStrength, kAimSpringDamping);
	private Spring mag_ground_pose_spring= new Spring(0,0,kAimSpringStrength, kAimSpringDamping);

	private bool left_hand_occupied= false;
	private const int kMaxHeadRecoil= 10;
	private float[] head_recoil_delay = new float[kMaxHeadRecoil];
	private int next_head_recoil_delay= 0;
	private Vector3 mag_ground_pos;
	private Quaternion mag_ground_rot;

	enum HandMagStage {HOLD, HOLD_TO_INSERT, EMPTY};
	private HandMagStage mag_stage= HandMagStage.EMPTY;

	private List<GameObject> collected_rounds = new List<GameObject>();

	private int target_weapon_slot= -2;
	private bool queue_drop= false;
	private List<GameObject> loose_bullets;
	private List<Spring> loose_bullet_spring;
	private Spring show_bullet_spring= new Spring(0,0,kAimSpringStrength, kAimSpringDamping);
	private float picked_up_bullet_delay= 0.0f;

	private float head_fall= 0.0f;
	private float head_fall_vel= 0.0f;
	private float head_tilt= 0.0f;
	private float head_tilt_vel= 0.0f;
	private float head_tilt_x_vel= 0.0f;
	private float head_tilt_y_vel= 0.0f;
	private float dead_fade= 1.0f;
	private float win_fade= 0.0f;
	private float dead_volume_fade= 0.0f;
	private bool dead_body_fell= false;

	private float start_tape_delay= 0.0f;
	private float stop_tape_delay= 0.0f;
	private List<AudioClip> tapes_heard= new List<AudioClip>();
	private List<AudioClip> tapes_remaining= new List<AudioClip>();
	private List<AudioClip> total_tapes= new List<AudioClip>();
	private bool tape_in_progress= false;
	private int unplayed_tapes= 0;

	private bool god_mode= false;
	private bool slomo_mode= false;
	private int iddqd_progress= 0;
	private int idkfa_progress= 0;
	private int slomo_progress= 0;
	private float cheat_delay= 0.0f;
	private float level_reset_hold= 0.0f;

	enum WeaponSlotType {GUN, MAGAZINE, FLASHLIGHT, EMPTY, EMPTYING};

	class WeaponSlot {
		public GameObject obj = null;
		public WeaponSlotType type = WeaponSlotType.EMPTY;
		public Vector3 start_pos = new Vector3(0,0,0);
		public Quaternion start_rot = Quaternion.identity;
		public Spring spring = new Spring(1,1,100,0.000001f);
	};

	private WeaponSlot[] weapon_slots = new WeaponSlot[10];

	private float health= 1.0f;
	private bool dying= false;
	private bool dead= false;
	private bool won= false;

	public bool IsAiming (){
		 return (gun_instance != null && aim_spring.target_state == 1.0f);
	}

	public bool IsDead (){
		 return dead;
	}

	public void  StepRecoil ( float amount  ){
		x_recoil_spring.vel += Random.Range(100,400) * amount;
		y_recoil_spring.vel += Random.Range(-200,200) * amount;
	}

	public void  WasShot (){
		head_recoil_spring_x.vel += Random.Range(-400,400);
		head_recoil_spring_y.vel += Random.Range(-400,400);
		x_recoil_spring.vel += Random.Range(-400,400);
		y_recoil_spring.vel += Random.Range(-400,400);
		rotation_x += Random.Range(-4,4);
		rotation_y += Random.Range(-4,4);
		if(!god_mode && !won){
			dying = true;
			if(Random.Range(0.0f,1.0f) < 0.3f){
				SetDead(true);
			}
			if(dead && Random.Range(0.0f,1.0f) < 0.3f){
				dead_fade += 0.3f;
			}
		}
	}

	public void  FallDeath ( Vector3 vel  ){
		if(!god_mode && !won){
			SetDead(true);
			head_fall_vel = vel.y;
			dead_fade = Mathf.Max(dead_fade, 0.5f);
			head_recoil_spring_x.vel += Random.Range(-400,400);
			head_recoil_spring_y.vel += Random.Range(-400,400);
		}
	}

	void  InstaKill (){
		SetDead(true);
		dead_fade = 1.0f;
	}

	public void  Shock (){
		if(!god_mode && !won){
			if(!dead){
				PlaySoundFromGroup(sound_electrocute, 1.0f);
			}
			SetDead(true);
		}
		head_recoil_spring_x.vel += Random.Range(-400,400);
		head_recoil_spring_y.vel += Random.Range(-400,400);
	}

	void  SetDead ( bool new_dead  ){
		 if(new_dead == dead){
			return;
		}
		dead = new_dead;
		if(!dead){
			head_tilt_vel = 0.0f;
			head_tilt_x_vel = 0.0f;
			head_tilt_y_vel = 0.0f;
			head_tilt = 0.0f;
			head_fall = 0.0f;
		} else {
			GetComponent<MusicScript>().HandleEvent(MusicEvent.DEAD);
			head_tilt_vel = Random.Range(-100,100);
			head_tilt_x_vel = Random.Range(-100,100);
			head_tilt_y_vel = Random.Range(-100,100);
			head_fall = 0.0f;
			head_fall_vel = 0.0f;
			dead_body_fell = false;
		}
	}

	void  PlaySoundFromGroup ( AudioClip[] group ,   float volume  ){
		int which_shot= Random.Range(0,group.Length);
		GetComponent<AudioSource>().PlayOneShot(group[which_shot], volume * PlayerPrefs.GetFloat("sound_volume", 1.0f));
	}

	void  AddLooseBullet ( bool spring  ){
		 loose_bullets.Add(Instantiate(casing_with_bullet));
		var new_spring= new Spring(0.3f,0.3f,kAimSpringStrength,kAimSpringDamping);
		loose_bullet_spring.Add(new_spring);
		if(spring){
			new_spring.vel = 3.0f;
			picked_up_bullet_delay = 2.0f;
		}
	}

	void  Start (){ 
		disable_springs = false; 
		disable_recoil = true;
		holder = GameObject.Find("gui_skin_holder").GetComponent<GUISkinHolder>();
		weapon_holder = holder.weapon.GetComponent<WeaponHolder>();
		magazine_obj = weapon_holder.mag_object;
		gun_obj = weapon_holder.gun_object;
		casing_with_bullet = weapon_holder.bullet_object;

		if(Random.Range(0.0f,1.0f) < 0.35f){
			held_flashlight = Instantiate(holder.flashlight_object);
			held_flashlight.GetComponent<FlashlightScript>().TurnOn();
			holder.has_flashlight = true;
		}
		
		rotation_x = transform.rotation.eulerAngles.y;
		view_rotation_x = transform.rotation.eulerAngles.y;
		gun_instance = Instantiate(gun_obj);
		var renderers= gun_instance.GetComponentsInChildren<Renderer>();
		foreach(Renderer renderer in renderers){
			renderer.shadowCastingMode = ShadowCastingMode.Off; 
		}
		main_camera = GameObject.Find("Main Camera").gameObject;
		character_controller = GetComponent<CharacterController>();
		for(var i=0; i<kMaxHeadRecoil; ++i){
			head_recoil_delay[i] = -1.0f;
		}
		for(var i=0; i<10; ++i){
			weapon_slots[i] = new WeaponSlot();
		}
		var num_start_bullets= Random.Range(0,10);
		if(GetGunScript().gun_type == GunType.AUTOMATIC){
			var num_start_mags= Random.Range(0,3);
			for(var i=1; i<num_start_mags+1; ++i){
				weapon_slots[i].type = WeaponSlotType.MAGAZINE;
				weapon_slots[i].obj = Instantiate(magazine_obj);
			}
		} else {
			num_start_bullets += Random.Range(0,20);
		}
		loose_bullets = new List<GameObject>();
		loose_bullet_spring = new List<Spring>();
		for(var i=0; i<num_start_bullets; ++i){
			AddLooseBullet(false);
		}
		audiosource_tape_background = gameObject.AddComponent<AudioSource>();
		audiosource_tape_background.loop = true;
		audiosource_tape_background.clip = holder.sound_tape_background;
		audiosource_audio_content = gameObject.AddComponent<AudioSource>();
		audiosource_audio_content.loop = false;
		
		//int count= 0;
		foreach(var tape in holder.sound_tape_content){
			total_tapes.Add(tape);
			/*++count;
			if(count >= 2){
				break;
			}*/
		}
		var temp_total_tapes= new List<AudioClip>(total_tapes);
		while(temp_total_tapes.Count > 0){
			var rand_tape_id= Random.Range(0,temp_total_tapes.Count);
			tapes_remaining.Add(temp_total_tapes[rand_tape_id]);
			temp_total_tapes.RemoveAt(rand_tape_id);
		}
	}

	float  GunDist (){
		return kGunDistance * (0.5f + PlayerPrefs.GetFloat("gun_distance", 1.0f)*0.5f);
	}

	Vector3 AimPos (){
		var aim_dir = AimDir();
		return main_camera.transform.position + aim_dir*GunDist();
	}

	Vector3 AimDir (){
		var aim_rot = Quaternion.Euler(-rotation_y, rotation_x, 0.0f);
		return aim_rot * new Vector3(0.0f,0.0f,1.0f);
	}

	GunScript GetGunScript (){
		return gun_instance.GetComponent<GunScript>();
	}

	Vector3 mix (  Vector3 a ,   Vector3 b ,   float val   ){
		return a + (b-a) * val;
	}

	Quaternion mix (  Quaternion a ,   Quaternion b ,   float val   ){
		float angle;
		Vector3 axis;
		(Quaternion.Inverse(b)*a).ToAngleAxis(out angle, out axis);
		if(angle > 180){
			angle -= 360;
		}
		if(angle < -180){
			angle += 360;
		}
		if(angle == 0){
			return a;
		}
		return a * Quaternion.AngleAxis(angle * -val, axis);
	}

	bool ShouldPickUpNearby (){
		var colliders= Physics.OverlapSphere(main_camera.transform.position, 2.0f, 1 << 8);
		foreach(var collider in colliders){
			if(magazine_obj && collider.gameObject.name == magazine_obj.name+"(Clone)" && collider.gameObject.GetComponent<Rigidbody>()){
				if(mag_stage == HandMagStage.EMPTY){
					return true;
				}	
			} else if((collider.gameObject.name == casing_with_bullet.name || collider.gameObject.name == casing_with_bullet.name+"(Clone)") && collider.gameObject.GetComponent<Rigidbody>()){
				return true;
			}
		}
		return false;
	}

	void  HandleGetControl (){
		GameObject nearest_mag = null;
		var nearest_mag_dist = 0.0f;
		var colliders = Physics.OverlapSphere(main_camera.transform.position, 2.0f, 1 << 8);
		foreach(var collider in colliders){
			if(magazine_obj && collider.gameObject.name == magazine_obj.name+"(Clone)" && collider.gameObject.GetComponent<Rigidbody>()){
				var dist= Vector3.Distance(collider.transform.position, main_camera.transform.position);
				if(!nearest_mag || dist < nearest_mag_dist){	
					nearest_mag_dist = dist;
					nearest_mag = collider.gameObject;
				}					
			} else if((collider.gameObject.name == casing_with_bullet.name || collider.gameObject.name == casing_with_bullet.name+"(Clone)") && collider.gameObject.GetComponent<Rigidbody>()){
				collected_rounds.Add(collider.gameObject);			
				collider.gameObject.GetComponent<Rigidbody>().useGravity = false;
				collider.gameObject.GetComponent<Rigidbody>().WakeUp();
				collider.enabled = false;
			} else if(collider.gameObject.name == "cassette_tape(Clone)" && collider.gameObject.GetComponent<Rigidbody>()){
				collected_rounds.Add(collider.gameObject);			
				collider.gameObject.GetComponent<Rigidbody>().useGravity = false;
				collider.gameObject.GetComponent<Rigidbody>().WakeUp();
				collider.enabled = false;
			} else if(collider.gameObject.name == "flashlight_object(Clone)" && collider.gameObject.GetComponent<Rigidbody>() && !held_flashlight){
				held_flashlight = collider.gameObject;
				held_flashlight.GetComponent<FlashlightScript>().TurnOn();
				holder.has_flashlight = true;
				flash_ground_pos = held_flashlight.transform.position;
				flash_ground_rot = held_flashlight.transform.rotation;
				flash_ground_pose_spring.state = 1.0f;
				flash_ground_pose_spring.vel = 1.0f;
			}
		}
		if(nearest_mag && mag_stage == HandMagStage.EMPTY){
			magazine_instance_in_hand = nearest_mag;
			Destroy(magazine_instance_in_hand.GetComponent<Rigidbody>());
			mag_ground_pos = magazine_instance_in_hand.transform.position;
			mag_ground_rot = magazine_instance_in_hand.transform.rotation;
			mag_ground_pose_spring.state = 1.0f;
			mag_ground_pose_spring.vel = 1.0f;
			hold_pose_spring.state = 1.0f;
			hold_pose_spring.vel = 0.0f;
			hold_pose_spring.target_state = 1.0f;
			mag_stage = HandMagStage.HOLD;
		}
	}

	bool HandleInventoryControls (){	
		 if(Input.GetButtonDown("Holster")){
			target_weapon_slot = -1;
		}
		if(Input.GetButtonDown("Inventory 1")){
			target_weapon_slot = 0;
		}
		if(Input.GetButtonDown("Inventory 2")){
			target_weapon_slot = 1;
		}
		if(Input.GetButtonDown("Inventory 3")){
			target_weapon_slot = 2;
		}
		if(Input.GetButtonDown("Inventory 4")){
			target_weapon_slot = 3;
		}
		if(Input.GetButtonDown("Inventory 5")){
			target_weapon_slot = 4;
		}
		if(Input.GetButtonDown("Inventory 6")){
			target_weapon_slot = 5;
		}
		if(Input.GetButtonDown("Inventory 7")){
			target_weapon_slot = 6;
		}
		if(Input.GetButtonDown("Inventory 8")){
			target_weapon_slot = 7;
		}
		if(Input.GetButtonDown("Inventory 9")){
			target_weapon_slot = 8;
		}
		if(Input.GetButtonDown("Inventory 10")){
			target_weapon_slot = 9;
		}
		
		var mag_ejecting= false;
		if(gun_instance && (gun_instance.GetComponent<GunScript>().IsMagCurrentlyEjecting() || gun_instance.GetComponent<GunScript>().ready_to_remove_mag)){
			mag_ejecting = true;
		}
		
		var insert_mag_with_number_key= false;
		
		if(target_weapon_slot != -2 && !mag_ejecting && (mag_stage == HandMagStage.EMPTY || mag_stage == HandMagStage.HOLD)){
			if(target_weapon_slot == -1 && !gun_instance){
				for(var i=0; i<10; ++i){
					if(weapon_slots[i].type == WeaponSlotType.GUN){
						target_weapon_slot = i;
						break;
					}
				}
			}
			if(mag_stage == HandMagStage.HOLD && target_weapon_slot != -1 && weapon_slots[target_weapon_slot].type == WeaponSlotType.EMPTY){
				// Put held mag in empty slot
				for(var i=0; i<10; ++i){
					if(weapon_slots[target_weapon_slot].type != WeaponSlotType.EMPTY && weapon_slots[target_weapon_slot].obj == magazine_instance_in_hand){
						weapon_slots[target_weapon_slot].type = WeaponSlotType.EMPTY;
					}
				}
				weapon_slots[target_weapon_slot].type = WeaponSlotType.MAGAZINE;
				weapon_slots[target_weapon_slot].obj = magazine_instance_in_hand;
				weapon_slots[target_weapon_slot].spring.state = 0.0f;
				weapon_slots[target_weapon_slot].spring.target_state = 1.0f;
				weapon_slots[target_weapon_slot].start_pos = magazine_instance_in_hand.transform.position - main_camera.transform.position;
				weapon_slots[target_weapon_slot].start_rot = Quaternion.Inverse(main_camera.transform.rotation) * magazine_instance_in_hand.transform.rotation;
				magazine_instance_in_hand = null;
				mag_stage = HandMagStage.EMPTY;
				target_weapon_slot = -2;
			} else if(mag_stage == HandMagStage.HOLD && target_weapon_slot != -1 && weapon_slots[target_weapon_slot].type == WeaponSlotType.EMPTYING && weapon_slots[target_weapon_slot].obj == magazine_instance_in_hand && gun_instance && !gun_instance.GetComponent<GunScript>().IsThereAMagInGun()){
				insert_mag_with_number_key = true;
				target_weapon_slot = -2;
			} else if (target_weapon_slot != -1 && mag_stage == HandMagStage.EMPTY && weapon_slots[target_weapon_slot].type == WeaponSlotType.MAGAZINE){
				// Take mag from inventory
				magazine_instance_in_hand = weapon_slots[target_weapon_slot].obj;
				mag_stage = HandMagStage.HOLD;
				hold_pose_spring.state = 1.0f;
				hold_pose_spring.target_state = 1.0f;
				weapon_slots[target_weapon_slot].type = WeaponSlotType.EMPTYING;
				weapon_slots[target_weapon_slot].spring.target_state = 0.0f;
				weapon_slots[target_weapon_slot].spring.state = 1.0f;
				target_weapon_slot = -2;
			} else if (target_weapon_slot != -1 && mag_stage == HandMagStage.EMPTY && weapon_slots[target_weapon_slot].type == WeaponSlotType.EMPTY && held_flashlight){
				// Put flashlight away
				held_flashlight.GetComponent<FlashlightScript>().TurnOff();
				weapon_slots[target_weapon_slot].type = WeaponSlotType.FLASHLIGHT;
				weapon_slots[target_weapon_slot].obj = held_flashlight;
				weapon_slots[target_weapon_slot].spring.state = 0.0f;
				weapon_slots[target_weapon_slot].spring.target_state = 1.0f;
				weapon_slots[target_weapon_slot].start_pos = held_flashlight.transform.position - main_camera.transform.position;
				weapon_slots[target_weapon_slot].start_rot = Quaternion.Inverse(main_camera.transform.rotation) * held_flashlight.transform.rotation;
				held_flashlight = null;
				target_weapon_slot = -2;
			}  else if (target_weapon_slot != -1 && !held_flashlight && weapon_slots[target_weapon_slot].type == WeaponSlotType.FLASHLIGHT){
				// Take flashlight from inventory
				held_flashlight = weapon_slots[target_weapon_slot].obj;
				held_flashlight.GetComponent<FlashlightScript>().TurnOn();
				weapon_slots[target_weapon_slot].type = WeaponSlotType.EMPTYING;
				weapon_slots[target_weapon_slot].spring.target_state = 0.0f;
				weapon_slots[target_weapon_slot].spring.state = 1.0f;
				target_weapon_slot = -2;
			} else if(gun_instance && target_weapon_slot == -1){
				// Put gun away
				if(target_weapon_slot == -1){
					for(var i=0; i<10; ++i){
						if(weapon_slots[i].type == WeaponSlotType.EMPTY){
							target_weapon_slot = i;
							break;
						}
					}
				}
				if(target_weapon_slot != -1 && weapon_slots[target_weapon_slot].type == WeaponSlotType.EMPTY){
					for(var i=0; i<10; ++i){
						if(weapon_slots[target_weapon_slot].type != WeaponSlotType.EMPTY && weapon_slots[target_weapon_slot].obj == gun_instance){
							weapon_slots[target_weapon_slot].type = WeaponSlotType.EMPTY;
						}
					}
					weapon_slots[target_weapon_slot].type = WeaponSlotType.GUN;
					weapon_slots[target_weapon_slot].obj = gun_instance;
					weapon_slots[target_weapon_slot].spring.state = 0.0f;
					weapon_slots[target_weapon_slot].spring.target_state = 1.0f;
					weapon_slots[target_weapon_slot].start_pos = gun_instance.transform.position - main_camera.transform.position;
					weapon_slots[target_weapon_slot].start_rot = Quaternion.Inverse(main_camera.transform.rotation) * gun_instance.transform.rotation;
					gun_instance = null;
					target_weapon_slot = -2;
				}
			} else if(target_weapon_slot >= 0 && !gun_instance){
				if(weapon_slots[target_weapon_slot].type == WeaponSlotType.EMPTY){
					target_weapon_slot = -2;
				} else {
					if(weapon_slots[target_weapon_slot].type == WeaponSlotType.GUN){
						gun_instance = weapon_slots[target_weapon_slot].obj;
						weapon_slots[target_weapon_slot].type = WeaponSlotType.EMPTYING;
						weapon_slots[target_weapon_slot].spring.target_state = 0.0f;
						weapon_slots[target_weapon_slot].spring.state = 1.0f;
						target_weapon_slot = -2;
					} else if(weapon_slots[target_weapon_slot].type == WeaponSlotType.MAGAZINE && mag_stage == HandMagStage.EMPTY){
						magazine_instance_in_hand = weapon_slots[target_weapon_slot].obj;
						mag_stage = HandMagStage.HOLD;
						weapon_slots[target_weapon_slot].type = WeaponSlotType.EMPTYING;
						weapon_slots[target_weapon_slot].spring.target_state = 0.0f;
						weapon_slots[target_weapon_slot].spring.state = 1.0f;
						target_weapon_slot = -2;
					}
				}
			}
		}
		return insert_mag_with_number_key;
	}

	void  HandleGunControls ( bool insert_mag_with_number_key  ){
		 var gun_script= GetGunScript();
		if(Input.GetButton("Trigger")){
			gun_script.ApplyPressureToTrigger();
		} else {
			gun_script.ReleasePressureFromTrigger();
		}
		if(Input.GetButtonDown("Slide Lock")){
			gun_script.ReleaseSlideLock();
		}
		if(Input.GetButtonUp("Slide Lock")){
			gun_script.ReleasePressureOnSlideLock();
		}
		if(Input.GetButton("Slide Lock")){
			gun_script.PressureOnSlideLock();
		}
		if(Input.GetButtonDown("Safety")){
			gun_script.ToggleSafety();			
		}	
		if(Input.GetButtonDown("Auto Mod Toggle")){
			gun_script.ToggleAutoMod();			
		}	
		if(Input.GetButtonDown("Pull Back Slide")){
			gun_script.PullBackSlide();
		}
		if(Input.GetButtonUp("Pull Back Slide")){
			gun_script.ReleaseSlide();
		}	
		if(Input.GetButtonDown("Swing Out Cylinder")){
			gun_script.SwingOutCylinder();
		}	
		if(Input.GetButtonDown("Close Cylinder")){
			gun_script.CloseCylinder();
		}	
		if(Input.GetButton("Extractor Rod")){
			gun_script.ExtractorRod();
		}
		if(Input.GetButton("Hammer")){
			gun_script.PressureOnHammer();
		}
		if(Input.GetButtonUp("Hammer")){
			gun_script.ReleaseHammer();
		}		
		if(Input.GetAxis("Mouse ScrollWheel") != 0){
			gun_script.RotateCylinder((int)Input.GetAxis("Mouse ScrollWheel"));
		}		
		if(Input.GetButtonDown("Insert")){
			if(loose_bullets.Count > 0){
				if(GetGunScript().AddRoundToCylinder()){
					var loose_bullet = loose_bullets[loose_bullets.Count-1];
					loose_bullets.RemoveAt(loose_bullets.Count-1);
					GameObject.Destroy(loose_bullet);
					loose_bullet_spring.RemoveAt(loose_bullet_spring.Count-1);
				}
			}
		}
		if(slide_pose_spring.target_state < 0.1f && reload_pose_spring.target_state < 0.1f){
			gun_tilt = GunTilt.CENTER;
		} else if(slide_pose_spring.target_state > reload_pose_spring.target_state){
			gun_tilt = GunTilt.LEFT;
		} else {
			gun_tilt = GunTilt.RIGHT;
		}
		
		slide_pose_spring.target_state = 0.0f;
		reload_pose_spring.target_state = 0.0f;
		press_check_pose_spring.target_state = 0.0f;
		
		if(gun_script.IsSafetyOn()){
			reload_pose_spring.target_state = 0.2f;
			slide_pose_spring.target_state = 0.0f;
			gun_tilt = GunTilt.RIGHT;
		}
		
		if(gun_script.IsSlideLocked()){
			if(gun_tilt != GunTilt.LEFT){
				reload_pose_spring.target_state = 0.7f;
			} else {
				slide_pose_spring.target_state = 0.7f;
			}
		}
		if(gun_script.IsSlidePulledBack()){
			if(gun_tilt != GunTilt.RIGHT){
				slide_pose_spring.target_state = 1.0f;
			} else {
				reload_pose_spring.target_state = 1.0f;
			}
		}
		if(gun_script.IsPressCheck()){
			slide_pose_spring.target_state = 0.0f;
			reload_pose_spring.target_state = 0.0f;
			press_check_pose_spring.target_state = 0.6f;
		}
		
		add_rounds_pose_spring.target_state = 0.0f;
		eject_rounds_pose_spring.target_state = 0.0f;
		inspect_cylinder_pose_spring.target_state = 0.0f;
		if(gun_script.IsEjectingRounds()){
			eject_rounds_pose_spring.target_state = 1.0f;
		//} else if(gun_script.IsAddingRounds()){
		//	add_rounds_pose_spring.target_state = 1.0f;
		} else if(gun_script.IsCylinderOpen()){
			inspect_cylinder_pose_spring.target_state = 1.0f;
		}
		
		x_recoil_spring.vel += gun_script.recoil_transfer_x;
		y_recoil_spring.vel += gun_script.recoil_transfer_y;
		rotation_x += gun_script.rotation_transfer_x;
		rotation_y += gun_script.rotation_transfer_y;
		gun_script.recoil_transfer_x = 0.0f;
		gun_script.recoil_transfer_y = 0.0f;
		gun_script.rotation_transfer_x = 0.0f;
		gun_script.rotation_transfer_y = 0.0f;
		if(gun_script.add_head_recoil){
			head_recoil_delay[next_head_recoil_delay] = 0.1f;
			next_head_recoil_delay = (next_head_recoil_delay + 1)%kMaxHeadRecoil;
			gun_script.add_head_recoil = false;
		}
		
		if(gun_script.ready_to_remove_mag && !magazine_instance_in_hand){
			magazine_instance_in_hand = gun_script.RemoveMag();
			mag_stage = HandMagStage.HOLD;
			hold_pose_spring.state = 0.0f;
			hold_pose_spring.vel = 0.0f;
			hold_pose_spring.target_state = 1.0f;
		}
		if((Input.GetButtonDown("Insert")/* && aim_spring.state > 0.5f*/) || insert_mag_with_number_key){
			if(mag_stage == HandMagStage.HOLD && !gun_script.IsThereAMagInGun() || insert_mag_with_number_key){
				hold_pose_spring.target_state = 0.0f;
				mag_stage = HandMagStage.HOLD_TO_INSERT;
			}
		}
		if(mag_stage == HandMagStage.HOLD_TO_INSERT){
			if(hold_pose_spring.state < 0.01f){
				gun_script.InsertMag(magazine_instance_in_hand);
				magazine_instance_in_hand = null;
				mag_stage = HandMagStage.EMPTY;
			}
		}
	}

	void  HandleControls (){
		if (Input.GetButton("Reset Tracking"))
		{
			UnityEngine.VR.InputTracking.Recenter();
			//OVRManager.display.RecenterPose();
		}

		if(Input.GetButton("Get")){
			HandleGetControl();
		}
		
		for(var i= 0; i < kMaxHeadRecoil; ++i){
			if(head_recoil_delay[i] != -1.0f){
				head_recoil_delay[i] -= Time.deltaTime;
				if(head_recoil_delay[i] <= 0.0f){
					head_recoil_spring_x.vel += Random.Range(-30.0f,30.0f);
					head_recoil_spring_y.vel += Random.Range(-30.0f,30.0f);
					head_recoil_delay[i] = -1.0f;
				}
			}
		}
		
		var insert_mag_with_number_key= HandleInventoryControls();
		
		if(Input.GetButtonDown("Eject/Drop") || queue_drop){
			if(mag_stage == HandMagStage.HOLD){
				mag_stage = HandMagStage.EMPTY;
				magazine_instance_in_hand.AddComponent<Rigidbody>();
				magazine_instance_in_hand.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
				magazine_instance_in_hand.GetComponent<Rigidbody>().velocity = character_controller.velocity;
				magazine_instance_in_hand = null;
				queue_drop = false;
			}
		}
		
		if(Input.GetButtonDown("Eject/Drop")){
			if(mag_stage == HandMagStage.EMPTY && gun_instance){
				if(gun_instance.GetComponent<GunScript>().IsMagCurrentlyEjecting()){
					queue_drop = true;
				} else {
					gun_instance.GetComponent<GunScript>().MagEject();
				}
			} else if(mag_stage == HandMagStage.HOLD_TO_INSERT){
				mag_stage = HandMagStage.HOLD;
				hold_pose_spring.target_state = 1.0f;
			}
		}
		
		if(gun_instance){
			HandleGunControls(insert_mag_with_number_key);
		} else if(mag_stage == HandMagStage.HOLD){
			if(Input.GetButtonDown("Insert")){
				if(loose_bullets.Count > 0){
					if(magazine_instance_in_hand.GetComponent<mag_script>().AddRound()){
						var loose_bullet = loose_bullets[loose_bullets.Count-1];
						loose_bullets.RemoveAt(loose_bullets.Count-1);
						GameObject.Destroy(loose_bullet);
						loose_bullet_spring.RemoveAt(loose_bullet_spring.Count-1);
					}
				}
			}
			if(Input.GetButtonDown("Pull Back Slide")){
				if(magazine_instance_in_hand.GetComponent<mag_script>().RemoveRoundAnimated()){
					AddLooseBullet(true);
					PlaySoundFromGroup(sound_bullet_grab, 0.2f);
				}
			}
		}
		
		if(Input.GetButtonDown("Aim Toggle")){
			aim_toggle = !aim_toggle;
		}
		if(Input.GetButtonDown("Slow Motion Toggle") && slomo_mode){
			if(Time.timeScale == 1.0f){
				Time.timeScale = 0.1f;
			} else {
				Time.timeScale = 1.0f;
			}
		}
	}

	void  StartTapePlay (){
		GetComponent<AudioSource>().PlayOneShot(holder.sound_tape_start, 1.0f * PlayerPrefs.GetFloat("voice_volume", 1.0f));
		audiosource_tape_background.Play();
		if(tape_in_progress && start_tape_delay == 0.0f){ 
			audiosource_audio_content.Play();
		}
		if(!tape_in_progress && tapes_remaining.Count > 0){
			audiosource_audio_content.clip = tapes_remaining[0];
			tapes_remaining.RemoveAt(0);
			//audiosource_audio_content.pitch = 10.0f;
			//audiosource_audio_content.clip = holder.sound_scream[Random.Range(0,holder.sound_scream.length)];
			start_tape_delay = Random.Range(0.5f,3.0f);
			stop_tape_delay = 0.0f;
			tape_in_progress = true;
		}
		audiosource_tape_background.pitch = 0.1f;
		audiosource_audio_content.pitch = 0.1f;
	}

	void  StopTapePlay (){
		GetComponent<AudioSource>().PlayOneShot(holder.sound_tape_end, 1.0f * PlayerPrefs.GetFloat("voice_volume", 1.0f));
		if(tape_in_progress){
			audiosource_tape_background.Pause();
			audiosource_audio_content.Pause();
		} else {
			audiosource_tape_background.Stop();
			audiosource_audio_content.Stop();
		}
	}

	void  StartWin (){
		GetComponent<MusicScript>().HandleEvent(MusicEvent.WON);
		won = true;
	}

	void  ApplyPose ( string name ,   float amount  ){
		var pose= gun_instance.transform.FindChild(name);
		if(amount == 0.0f || !pose){
			return;
		}
		gun_instance.transform.position = mix(gun_instance.transform.position,
											  pose.position,
											  amount);
		gun_instance.transform.rotation = mix(
			gun_instance.transform.rotation,
			pose.rotation,
			amount);
	}

	void  UpdateCheats (){
		if(iddqd_progress == 0 && Input.GetKeyDown(KeyCode.I)){
			++iddqd_progress; cheat_delay = 1.0f;
		} else if(iddqd_progress == 1 && Input.GetKeyDown(KeyCode.D)){
			++iddqd_progress; cheat_delay = 1.0f;
		} else if(iddqd_progress == 2 && Input.GetKeyDown(KeyCode.D)){
			++iddqd_progress; cheat_delay = 1.0f;
		} else if(iddqd_progress == 3 && Input.GetKeyDown(KeyCode.Q)){
			++iddqd_progress; cheat_delay = 1.0f;
		} else if(iddqd_progress == 4 && Input.GetKeyDown(KeyCode.D)){
			iddqd_progress = 0;
			god_mode = !god_mode; 
			PlaySoundFromGroup(holder.sound_scream, 1.0f);
		}
		if(idkfa_progress == 0 && Input.GetKeyDown(KeyCode.I)){
			++idkfa_progress; cheat_delay = 1.0f;
		} else if(idkfa_progress == 1 && Input.GetKeyDown(KeyCode.D)){
			++idkfa_progress; cheat_delay = 1.0f;
		} else if(idkfa_progress == 2 && Input.GetKeyDown(KeyCode.K)){
			++idkfa_progress; cheat_delay = 1.0f;
		} else if(idkfa_progress == 3 && Input.GetKeyDown(KeyCode.F)){
			++idkfa_progress; cheat_delay = 1.0f;
		} else if(idkfa_progress == 4 && Input.GetKeyDown(KeyCode.A)){
			idkfa_progress = 0;
			if(loose_bullets.Count < 30){
				PlaySoundFromGroup(sound_bullet_grab, 0.2f);
			}
			while(loose_bullets.Count < 30){
				AddLooseBullet(true);
			}
			PlaySoundFromGroup(holder.sound_scream, 1.0f);
		}
		if(slomo_progress == 0 && Input.GetKeyDown(KeyCode.S)){
			++slomo_progress; cheat_delay = 1.0f;
		} else if(slomo_progress == 1 && Input.GetKeyDown(KeyCode.L)){
			++slomo_progress; cheat_delay = 1.0f;
		} else if(slomo_progress == 2 && Input.GetKeyDown(KeyCode.O)){
			++slomo_progress; cheat_delay = 1.0f;
		} else if(slomo_progress == 3 && Input.GetKeyDown(KeyCode.M)){
			++slomo_progress; cheat_delay = 1.0f;
		} else if(slomo_progress == 4 && Input.GetKeyDown(KeyCode.O)){
			slomo_progress = 0;
			slomo_mode = true;
			if(Time.timeScale == 1.0f){
				Time.timeScale = 0.1f;
			} else {
				Time.timeScale = 1.0f;
			}
			PlaySoundFromGroup(holder.sound_scream, 1.0f);
		}
		if(cheat_delay > 0.0f){
			cheat_delay -= Time.deltaTime;
			if(cheat_delay <= 0.0f){
				cheat_delay = 0.0f;
				iddqd_progress = 0;
				idkfa_progress = 0;
				slomo_progress = 0;
			}
		}
	}

	void  UpdateTape (){
		if(!tape_in_progress && unplayed_tapes > 0){
			--unplayed_tapes;
			StartTapePlay();
		}
		if(Input.GetButtonDown("Tape Player") && tape_in_progress){
			if(!audiosource_tape_background.isPlaying){
				StartTapePlay();
			} else {
				StopTapePlay();
			}
		}
		if(tape_in_progress && audiosource_tape_background.isPlaying){ 
			GetComponent<MusicScript>().SetMystical((tapes_heard.Count+1.0f)/total_tapes.Count);
			audiosource_tape_background.volume = PlayerPrefs.GetFloat("voice_volume", 1.0f);
			audiosource_tape_background.pitch = Mathf.Min(1.0f,audiosource_audio_content.pitch + Time.deltaTime * 3.0f);
			audiosource_audio_content.volume = PlayerPrefs.GetFloat("voice_volume", 1.0f);
			audiosource_audio_content.pitch = Mathf.Min(1.0f,audiosource_audio_content.pitch + Time.deltaTime * 3.0f);
			//audiosource_audio_content.pitch = 10.0f;
			//audiosource_audio_content.volume = 0.1f;
			if(start_tape_delay > 0.0f){
				if(!audiosource_audio_content.isPlaying){
					start_tape_delay = Mathf.Max(0.0f, start_tape_delay - Time.deltaTime);
					if(start_tape_delay == 0.0f){
						audiosource_audio_content.Play();
					}
				}
			} else if(stop_tape_delay > 0.0f){
				stop_tape_delay = Mathf.Max(0.0f, stop_tape_delay - Time.deltaTime);
				if(stop_tape_delay == 0.0f){
					tape_in_progress = false;
					tapes_heard.push(audiosource_audio_content.clip);
					StopTapePlay();
					if(tapes_heard.Count == total_tapes.Count){
						StartWin();
					}
				}
			} else if(!audiosource_audio_content.isPlaying){
				stop_tape_delay = Random.Range(0.5f,3.0f);
			}
		}
	}

	void  UpdateHealth (){
		if(dying){
			health -= Time.deltaTime;
		}
		if(health <= 0.0f){
			health = 0.0f;
			SetDead(true);
			dying = false;
		}
	}

	void  UpdateHelpToggle (){
		if(Input.GetButton("Help Toggle")){
			help_hold_time += Time.deltaTime;
			if(show_help && help_hold_time >= 1.0f){
				show_advanced_help = true;
			}
		}
		if(Input.GetButtonDown("Help Toggle")){
			if(!show_help){
				show_help = true;
				help_ever_shown = true;
				just_started_help = true;
			}
			help_hold_time = 0.0f;
		}
		if(Input.GetButtonUp("Help Toggle")){
			if(show_help && help_hold_time < 1.0f && !just_started_help){
				show_help = false;
				show_advanced_help = false;
			}
			just_started_help = false;
		}
	}

	void  UpdateLevelResetButton (){
		if(Input.GetButtonDown("Level Reset")){
			level_reset_hold = 0.01f;
		}
		if(level_reset_hold != 0.0f && Input.GetButton("Level Reset")){
			level_reset_hold += Time.deltaTime; 
			dead_volume_fade = Mathf.Min(1.0f-level_reset_hold * 0.5f, dead_volume_fade);
			dead_fade = level_reset_hold * 0.5f;
			if(level_reset_hold >= 2.0f){
				Application.LoadLevel(Application.loadedLevel);
				level_reset_hold = 0.0f;
			}
		} else {
			level_reset_hold = 0.0f;
		}
	}

	void  UpdateLevelEndEffects (){
		if(won){
			win_fade = Mathf.Min(1.0f, win_fade + Time.deltaTime * 0.1f);
			dead_volume_fade = Mathf.Max(0.0f, dead_volume_fade - Time.deltaTime * 0.1f);
		} else if(dead){
			dead_fade = Mathf.Min(1.0f, dead_fade + Time.deltaTime * 0.3f);
			dead_volume_fade = Mathf.Max(0.0f, dead_volume_fade - Time.deltaTime * 0.23f);
			head_fall_vel -= 9.8f * Time.deltaTime;
			head_fall += head_fall_vel * Time.deltaTime;
			head_tilt += head_tilt_vel * Time.deltaTime;
			view_rotation_x += head_tilt_x_vel * Time.deltaTime;
			view_rotation_y += head_tilt_y_vel * Time.deltaTime;
			var min_fall= character_controller.height * character_controller.transform.localScale.y * -1.0f;
			if(head_fall < min_fall && head_fall_vel < 0.0f){			
				if(Mathf.Abs(head_fall_vel) > 0.5f){
					head_recoil_spring_x.vel += Random.Range(-10,10) * Mathf.Abs(head_fall_vel);
					head_recoil_spring_y.vel += Random.Range(-10,10) * Mathf.Abs(head_fall_vel);
					head_tilt_vel = 0.0f;
					head_tilt_x_vel = 0.0f;
					head_tilt_y_vel = 0.0f;
					if(!dead_body_fell){
						PlaySoundFromGroup(sound_body_fall, 1.0f);
						dead_body_fell = true;
					}
				}
				head_fall_vel *= -0.3f;
			}
			head_fall = Mathf.Max(min_fall,head_fall);
		} else {
			dead_fade = Mathf.Max(0.0f, dead_fade - Time.deltaTime * 1.5f);
			dead_volume_fade = Mathf.Min(1.0f, dead_volume_fade + Time.deltaTime * 1.5f);
		}
	}

	void  UpdateLevelChange (){
		if((dead && dead_volume_fade <= 0.0f)){ 
			Application.LoadLevel(Application.loadedLevel);
		}
		if(won && dead_volume_fade <= 0.0f){ 
			Application.LoadLevel("winscene");
		}
	}

	void  UpdateFallOffMapDeath (){
		if(transform.position.y < -1){
			InstaKill();
		}
	}

	void  UpdateAimSpring (){
		var offset_aim_target= false;
		if((Input.GetButton("Hold To Aim") || aim_toggle) && !dead && gun_instance){
			aim_spring.target_state = 1.0f;
			RaycastHit hit;
			if(Physics.Linecast(main_camera.transform.position, AimPos() + AimDir() * 0.2f, out hit, 1 << 0)){
				aim_spring.target_state = Mathf.Clamp(
					1.0f - (Vector3.Distance(hit.point, main_camera.transform.position)/(GunDist() + 0.2f)),
					0.0f,
					1.0f);
				offset_aim_target = true;
			}
		} else {
			aim_spring.target_state = 0.0f;
		}
		aim_spring.Update();
		if(offset_aim_target){
			aim_spring.target_state = 1.0f;
		}
	}

	void  UpdateCameraRotationControls (){
		rotation_y_min_leeway = Mathf.Lerp(0.0f,kRotationYMinLeeway,aim_spring.state);
		rotation_y_max_leeway = Mathf.Lerp(0.0f,kRotationYMaxLeeway,aim_spring.state);
		rotation_x_leeway = Mathf.Lerp(0.0f,kRotationXLeeway,aim_spring.state);
		
		if(PlayerPrefs.GetInt("lock_gun_to_center", 0)==1){
			rotation_y_min_leeway = 0;
			rotation_y_max_leeway = 0;
			rotation_x_leeway = 0;
		}
		
		sensitivity_x = PlayerPrefs.GetFloat("mouse_sensitivity", 1.0f) * 10.0f;
		sensitivity_y = PlayerPrefs.GetFloat("mouse_sensitivity", 1.0f) * 10.0f;
		if(PlayerPrefs.GetInt("mouse_invert", 0) == 1){
			sensitivity_y = -Mathf.Abs(sensitivity_y);
		} else {
			sensitivity_y = Mathf.Abs(sensitivity_y);
		}
		
		var in_menu= GameObject.Find("gui_skin_holder").GetComponent<optionsmenuscript>().IsMenuShown();
		if(!dead && !in_menu){
			rotation_x += Input.GetAxis("Mouse X") * sensitivity_x;
			rotation_y += Input.GetAxis("Mouse Y") * sensitivity_y;
			rotation_y = Mathf.Clamp (rotation_y, min_angle_y, max_angle_y);
		
			if((Input.GetButton("Hold To Aim") || aim_toggle) && gun_instance){
				//view_rotation_y = Mathf.Clamp(view_rotation_y, rotation_y - rotation_y_min_leeway, rotation_y + rotation_y_max_leeway);
				view_rotation_x = Mathf.Clamp(view_rotation_x, rotation_x - rotation_x_leeway, rotation_x + rotation_x_leeway);
			} else {
				view_rotation_x += Input.GetAxis("Mouse X") * sensitivity_x;
				//view_rotation_y += Input.GetAxis("Mouse Y") * sensitivity_y;
				//view_rotation_y = Mathf.Clamp (view_rotation_y, min_angle_y, max_angle_y);
				
				rotation_y = Mathf.Clamp(rotation_y, view_rotation_y - rotation_y_max_leeway, view_rotation_y + rotation_y_min_leeway);
				rotation_x = Mathf.Clamp(rotation_x, view_rotation_x - rotation_x_leeway, view_rotation_x + rotation_x_leeway);
			}
		}
	}

	void  UpdateCameraAndPlayerTransformation (){
		main_camera.transform.localEulerAngles = new Vector3(-view_rotation_y, view_rotation_x, head_tilt);
		if(!disable_recoil){
			main_camera.transform.localEulerAngles += new Vector3(head_recoil_spring_y.state, head_recoil_spring_x.state, 0); 
		}
		var ccEuler = character_controller.transform.localEulerAngles;
		ccEuler.y = view_rotation_x;
		character_controller.transform.localEulerAngles = ccEuler;
		var camPos = transform.position;
		camPos.y += character_controller.height *.5f * character_controller.transform.localScale.y - 0.1f;
		camPos.y += head_fall;
		main_camera.transform.position = camPos;
	}

	void  UpdateGunTransformation (){
		var aim_dir= AimDir();
		var aim_pos= AimPos();	
		
		var unaimed_dir= (transform.forward + new Vector3(0,-1,0)).normalized;
		var unaimed_pos= main_camera.transform.position + unaimed_dir*GunDist();
		 
		if(disable_springs){ 
			gun_instance.transform.position = mix(unaimed_pos, aim_pos, aim_spring.target_state);
			gun_instance.transform.forward = mix(unaimed_dir, aim_dir, aim_spring.target_state);
		} else { 
			gun_instance.transform.position = mix(unaimed_pos, aim_pos, aim_spring.state);
			gun_instance.transform.forward = mix(unaimed_dir, aim_dir, aim_spring.state);
		}
		
		if(disable_springs) {
			ApplyPose("pose_slide_pull", slide_pose_spring.target_state);
			ApplyPose("pose_reload", reload_pose_spring.target_state);
			ApplyPose("pose_press_check", press_check_pose_spring.target_state);
			ApplyPose("pose_inspect_cylinder", inspect_cylinder_pose_spring.target_state);
			ApplyPose("pose_add_rounds", add_rounds_pose_spring.target_state);
			ApplyPose("pose_eject_rounds", eject_rounds_pose_spring.target_state); 
		} else {
			ApplyPose("pose_slide_pull", slide_pose_spring.state);
			ApplyPose("pose_reload", reload_pose_spring.state);
			ApplyPose("pose_press_check", press_check_pose_spring.state);
			ApplyPose("pose_inspect_cylinder", inspect_cylinder_pose_spring.state);
			ApplyPose("pose_add_rounds", add_rounds_pose_spring.state);
			ApplyPose("pose_eject_rounds", eject_rounds_pose_spring.state); 
		}
		
		if(!disable_recoil){		
			gun_instance.transform.RotateAround(
				gun_instance.transform.FindChild("point_recoil_rotate").position,
				gun_instance.transform.rotation * new Vector3(1,0,0),
				x_recoil_spring.state);
				
			gun_instance.transform.RotateAround(
				gun_instance.transform.FindChild("point_recoil_rotate").position,
				new Vector3(0,1,0),
				y_recoil_spring.state); 
		}
	}

	void  UpdateFlashlightTransformation (){
		var flashlight_hold_pos= main_camera.transform.position + main_camera.transform.rotation*new Vector3(-0.15f,-0.01f,0.15f);
		var flashlight_hold_rot= main_camera.transform.rotation;
		
		var flashlight_pos= flashlight_hold_pos;
		var flashlight_rot= flashlight_hold_rot;

		held_flashlight.transform.position = flashlight_pos;
		held_flashlight.transform.rotation = flashlight_rot;

		held_flashlight.transform.RotateAround(
			held_flashlight.transform.FindChild("point_recoil_rotate").position,
			held_flashlight.transform.rotation * new Vector3(1,0,0),
			x_recoil_spring.state * 0.3f);
			
		held_flashlight.transform.RotateAround(
			held_flashlight.transform.FindChild("point_recoil_rotate").position,
			new Vector3(0,1,0),
			y_recoil_spring.state * 0.3f);

		flashlight_pos = held_flashlight.transform.position;
		flashlight_rot = held_flashlight.transform.rotation;
			
		if(gun_instance){
			flashlight_aim_pos = gun_instance.transform.position + gun_instance.transform.rotation*new Vector3(0.07f,-0.03f,0.0f);
			flashlight_aim_rot = gun_instance.transform.rotation;
			
			flashlight_aim_pos -= main_camera.transform.position;
			flashlight_aim_pos = Quaternion.Inverse(main_camera.transform.rotation) * flashlight_aim_pos;
			flashlight_aim_rot = Quaternion.Inverse(main_camera.transform.rotation) * flashlight_aim_rot;
		}
		
		if(disable_springs){
			flashlight_pos = mix(flashlight_pos, main_camera.transform.rotation * flashlight_aim_pos + main_camera.transform.position, aim_spring.target_state);
			flashlight_rot = mix(flashlight_rot, main_camera.transform.rotation * flashlight_aim_rot, aim_spring.target_state);
		} else {
			flashlight_pos = mix(flashlight_pos, main_camera.transform.rotation * flashlight_aim_pos + main_camera.transform.position, aim_spring.state);
			flashlight_rot = mix(flashlight_rot, main_camera.transform.rotation * flashlight_aim_rot, aim_spring.state);
		} 
		 
		var flashlight_mouth_pos= main_camera.transform.position + main_camera.transform.rotation*new Vector3(0.0f,-0.08f,0.05f);
		var flashlight_mouth_rot= main_camera.transform.rotation;
		
		flashlight_mouth_spring.target_state = 0.0f;
		if(magazine_instance_in_hand){
			flashlight_mouth_spring.target_state = 1.0f;
		}
		flashlight_mouth_spring.target_state = Mathf.Max(flashlight_mouth_spring.target_state,
			(inspect_cylinder_pose_spring.state + eject_rounds_pose_spring.state + (press_check_pose_spring.state/0.6f) + (reload_pose_spring.state/0.7f) + slide_pose_spring.state) * aim_spring.state);
		
		flashlight_mouth_spring.Update();
		 
		if(disable_springs){
			flashlight_pos = mix(flashlight_pos, flashlight_mouth_pos, flashlight_mouth_spring.target_state);
			flashlight_rot = mix(flashlight_rot, flashlight_mouth_rot, flashlight_mouth_spring.target_state);
			
			flashlight_pos = mix(flashlight_pos, flash_ground_pos, flash_ground_pose_spring.target_state);
			flashlight_rot = mix(flashlight_rot, flash_ground_rot, flash_ground_pose_spring.target_state);
		} else {
			flashlight_pos = mix(flashlight_pos, flashlight_mouth_pos, flashlight_mouth_spring.state);
			flashlight_rot = mix(flashlight_rot, flashlight_mouth_rot, flashlight_mouth_spring.state);
			
			flashlight_pos = mix(flashlight_pos, flash_ground_pos, flash_ground_pose_spring.state);
			flashlight_rot = mix(flashlight_rot, flash_ground_rot, flash_ground_pose_spring.state);
		}
			
		held_flashlight.transform.position = flashlight_pos;
		held_flashlight.transform.rotation = flashlight_rot;
	}

	void  UpdateMagazineTransformation (){
		if(gun_instance){
			mag_pos = gun_instance.transform.position;
			mag_rot = gun_instance.transform.rotation;
			mag_pos += (gun_instance.transform.FindChild("point_mag_to_insert").position - 
						gun_instance.transform.FindChild("point_mag_inserted").position);
		}
	   if(mag_stage == HandMagStage.HOLD || mag_stage == HandMagStage.HOLD_TO_INSERT){
			var mag_script= magazine_instance_in_hand.GetComponent<mag_script>();
			var hold_pos= main_camera.transform.position + main_camera.transform.rotation*mag_script.hold_offset;
			var hold_rot= main_camera.transform.rotation * Quaternion.AngleAxis(mag_script.hold_rotation.x, new Vector3(0,1,0)) * Quaternion.AngleAxis(mag_script.hold_rotation.y, new Vector3(1,0,0));
			if(disable_springs){ 
				hold_pos = mix(hold_pos, mag_ground_pos, mag_ground_pose_spring.target_state);
				hold_rot = mix(hold_rot, mag_ground_rot, mag_ground_pose_spring.target_state);
			} else {
				hold_pos = mix(hold_pos, mag_ground_pos, mag_ground_pose_spring.state);
				hold_rot = mix(hold_rot, mag_ground_rot, mag_ground_pose_spring.state);
			}
			if(hold_pose_spring.state != 1.0f){ 
				var amount= hold_pose_spring.state;
				if(disable_springs){ 
					amount = hold_pose_spring.target_state;
				}
				magazine_instance_in_hand.transform.position = mix(mag_pos, hold_pos, amount);
				magazine_instance_in_hand.transform.rotation = mix(mag_rot, hold_rot, amount);
			} else {
				magazine_instance_in_hand.transform.position = hold_pos;
				magazine_instance_in_hand.transform.rotation = hold_rot;
			}
		} else {
			magazine_instance_in_hand.transform.position = mag_pos;
			magazine_instance_in_hand.transform.rotation = mag_rot;
		} 
	}

	void  UpdateInventoryTransformation (){
		for(var i=0; i<10; ++i){
			var slot= weapon_slots[i];
			if(slot.type == WeaponSlotType.EMPTY){
				continue;
			}
			slot.obj.transform.localScale = new Vector3(1.0f,1.0f,1.0f); 
		}
		for(var i=0; i<10; ++i){
			var slot = weapon_slots[i];
			if(slot.type == WeaponSlotType.EMPTY){
				continue;
			}
			var start_pos= main_camera.transform.position + slot.start_pos;
			var start_rot= main_camera.transform.rotation * slot.start_rot;
			if(slot.type == WeaponSlotType.EMPTYING){
				start_pos = slot.obj.transform.position;
				start_rot = slot.obj.transform.rotation;
				if(Mathf.Abs(slot.spring.vel) <= 0.01f && slot.spring.state <= 0.01f){
					slot.type = WeaponSlotType.EMPTY;
					slot.spring.state = 0.0f;
				}
			} 
			var scale= 0.0f;
			if(disable_springs){  
				slot.obj.transform.position = mix(
					start_pos, 
					main_camera.transform.position/* + main_camera.GetComponent<Camera>().ScreenPointToRay(Vector3(main_camera.GetComponent<Camera>().pixelWidth * (0.05f + i*0.15f), main_camera.GetComponent<Camera>().pixelHeight * 0.17f,0)).direction * 0.3f*/, 
					slot.spring.target_state);
				scale = 0.3f * slot.spring.target_state + (1.0f - slot.spring.target_state);
				slot.obj.transform.localScale *= scale;
				slot.obj.transform.rotation = mix(
					start_rot, 
					main_camera.transform.rotation * Quaternion.AngleAxis(90, new Vector3(0,1,0)), 
					slot.spring.target_state);
			} else {  
				slot.obj.transform.position = mix(
					start_pos, 
					main_camera.transform.position/* + main_camera.GetComponent<Camera>().ScreenPointToRay(Vector3(main_camera.GetComponent<Camera>().pixelWidth * (0.05f + i*0.15f), main_camera.GetComponent<Camera>().pixelHeight * 0.17f,0)).direction * 0.3f*/, 
					slot.spring.state);
				scale = 0.3f * slot.spring.state + (1.0f - slot.spring.state);
				slot.obj.transform.localScale *= scale;
				slot.obj.transform.rotation = mix(
					start_rot, 
					main_camera.transform.rotation * Quaternion.AngleAxis(90, new Vector3(0,1,0)), 
					slot.spring.state);
			}
			var renderers= slot.obj.GetComponentsInChildren<Renderer>();
			foreach(Renderer renderer in renderers){
				renderer.shadowCastingMode = ShadowCastingMode.Off; 
			}
			slot.spring.Update();
		}
	}

	void  UpdateLooseBulletDisplay (){
		var revolver_open= (gun_instance && gun_instance.GetComponent<GunScript>().IsCylinderOpen());
		if((mag_stage == HandMagStage.HOLD && !gun_instance) || picked_up_bullet_delay > 0.0f || revolver_open){
			show_bullet_spring.target_state = 1.0f;
			picked_up_bullet_delay = Mathf.Max(0.0f, picked_up_bullet_delay - Time.deltaTime);
		} else {	
			show_bullet_spring.target_state = 0.0f;
		}
		show_bullet_spring.Update();

		for(var i=0; i<loose_bullets.Count; ++i){
			Spring spring = loose_bullet_spring[i];
			spring.Update();
			GameObject bullet = loose_bullets[i];
			bullet.transform.position = main_camera.transform.position/* + main_camera.GetComponent<Camera>().ScreenPointToRay(Vector3(0.0f, main_camera.GetComponent<Camera>().pixelHeight,0)).direction * 0.3f*/;
			bullet.transform.position += main_camera.transform.rotation * new Vector3(0.02f,-0.01f,0);
			bullet.transform.position += main_camera.transform.rotation * new Vector3(0.006f * i,0.0f,0);
			bullet.transform.position += main_camera.transform.rotation * new Vector3(-0.03f,0.03f,0) * (1.0f - show_bullet_spring.state);
			bullet.transform.localScale = new Vector3(spring.state, spring.state, spring.state);
			bullet.transform.rotation = main_camera.transform.rotation * Quaternion.AngleAxis(90, new Vector3(-1,0,0));
			//[DA] needs optimization
			/*var renderers= bullet.GetComponentsInChildren<Renderer>();
			foreach(Renderer renderer in renderers){
				renderer.castShadows = false; 
			}*/
		}
	}

	void  UpdateSprings (){	
		slide_pose_spring.Update();
		reload_pose_spring.Update();
		press_check_pose_spring.Update();
		inspect_cylinder_pose_spring.Update();
		add_rounds_pose_spring.Update();
		eject_rounds_pose_spring.Update();
		x_recoil_spring.Update();
		y_recoil_spring.Update();
		head_recoil_spring_x.Update();
		head_recoil_spring_y.Update();
		if(mag_stage == HandMagStage.HOLD || mag_stage == HandMagStage.HOLD_TO_INSERT){
			hold_pose_spring.Update();
			mag_ground_pose_spring.Update();
		}
		flash_ground_pose_spring.Update();
	}

	void  UpdatePickupMagnet (){
		var attract_pos= transform.position - new Vector3(0,character_controller.height * 0.2f,0);
		for(var i=0; i<collected_rounds.Count; ++i){
			var round= collected_rounds[i];
			if(!round){
				continue;
			}
			round.GetComponent<Rigidbody>().velocity += (attract_pos - round.transform.position) * Time.deltaTime * 20.0f;
			round.GetComponent<Rigidbody>().velocity *= Mathf.Pow(0.1f, Time.deltaTime);;
			//round.rigidbody.position += round.rigidbody.velocity * Time.deltaTime;
			if(Vector3.Distance(round.transform.position, attract_pos) < 0.5f){
				if(round.gameObject.name == "cassette_tape(Clone)"){
					++unplayed_tapes;
				} else {
					AddLooseBullet(true);
					PlaySoundFromGroup(sound_bullet_grab, 0.2f);
				}
				GameObject.Destroy(round);
			}
		}
		collected_rounds.remove(null);
	}

	void  Update (){
		UpdateTape();
		UpdateCheats();
		UpdateFallOffMapDeath();
		UpdateHealth();
		UpdateHelpToggle();	
		UpdateLevelResetButton();
		UpdateLevelChange();
		UpdateLevelEndEffects();
		AudioListener.volume = dead_volume_fade * PlayerPrefs.GetFloat("master_volume", 1.0f);
		UpdateAimSpring();
		UpdateCameraRotationControls();
		UpdateCameraAndPlayerTransformation();	
		if(gun_instance){
			UpdateGunTransformation();
		}
		if(held_flashlight){
			UpdateFlashlightTransformation();
		}				
		if(magazine_instance_in_hand){
			UpdateMagazineTransformation();
		}
		UpdateInventoryTransformation();
		UpdateLooseBulletDisplay();
		var in_menu= GameObject.Find("gui_skin_holder").GetComponent<optionsmenuscript>().IsMenuShown();
		if(!dead && !in_menu){
			HandleControls();
		}
		UpdateSprings();	
		UpdatePickupMagnet();
	}

	//void  FixedUpdate (){
	//}

	bool ShouldHolsterGun (){
		if(loose_bullets == null){
			return true;
		}
		if(loose_bullets.Count > 0){
		} else return false;
		if(magazine_instance_in_hand){
		} else return false;
		if(magazine_instance_in_hand.GetComponent<mag_script>().NumRounds() == 0){
		} else return false;
		return true;
	}

	bool CanLoadBulletsInMag (){
		 return !gun_instance && mag_stage == HandMagStage.HOLD && loose_bullets.Count > 0 && !magazine_instance_in_hand.GetComponent<mag_script>().IsFull();
	}

	bool CanRemoveBulletFromMag (){
		 return !gun_instance && mag_stage == HandMagStage.HOLD && magazine_instance_in_hand.GetComponent<mag_script>().NumRounds() > 0;
	}

	bool ShouldDrawWeapon (){
		 return !gun_instance && !CanLoadBulletsInMag();
	}

	int GetMostLoadedMag (){
		var max_rounds= 0;
		var max_rounds_slot= -1;
		for(var i=0; i<10; ++i){
			if(weapon_slots[i].type == WeaponSlotType.MAGAZINE){
				var rounds= weapon_slots[i].obj.GetComponent<mag_script>().NumRounds();
				if(rounds > max_rounds){
					max_rounds_slot = i+1;
					max_rounds = rounds;
				}
			}
		}
		return max_rounds_slot;
	}

	bool ShouldPutMagInInventory (){
		 var rounds= magazine_instance_in_hand.GetComponent<mag_script>().NumRounds();
		var most_loaded= GetMostLoadedMag();
		if(most_loaded == -1){
			return false;
		}
		if(weapon_slots[most_loaded-1].obj.GetComponent<mag_script>().NumRounds() > rounds){
			return true;
		}
		return false;
	}

	int GetEmptySlot (){
		var empty_slot= -1;
		for(var i=0; i<10; ++i){
			if(weapon_slots[i].type == WeaponSlotType.EMPTY){
				empty_slot = i+1;
				break;
			}
		}
		return empty_slot;
	}

	int GetFlashlightSlot (){
		var flashlight_slot= -1;
		for(var i=0; i<10; ++i){
			if(weapon_slots[i].type == WeaponSlotType.FLASHLIGHT){
				flashlight_slot = i+1;
				break;
			}
		}
		return flashlight_slot;
	}

	class DisplayLine {
		public string str;
		public bool  bold;
		public DisplayLine ( string _str ,   bool _bold  ){
			bold = _bold;
			str = _str;
		}
	};

	//[DA] TODO: remove as many allocations as possible from here, or simply skip this whole thing in VR mode

	void  OnGUI_ (){
		var display_text= new List<DisplayLine>();
		GunScript gun_script = null;
		if(gun_instance){
			gun_script = gun_instance.GetComponent<GunScript>();
		}
		display_text.push(new DisplayLine(tapes_heard.Count + " tapes absorbed out of "+total_tapes.Count, true));
		if(!show_help){
			display_text.push(new DisplayLine("View help: Press [ ? ]", !help_ever_shown));
		} else {
			display_text.push(new DisplayLine("Hide help: Press [ ? ]", false));
			display_text.push(new DisplayLine("", false));
			if(tape_in_progress){
				display_text.push(new DisplayLine("Pause/Resume tape player: [ x ]", false));
			}
			
			display_text.push(new DisplayLine("Look: [ move mouse ]", false));
			display_text.push(new DisplayLine("Move: [ WASD ]", false));
			display_text.push(new DisplayLine("Jump: [ space ]", false));
			display_text.push(new DisplayLine("Pick up nearby: hold [ g ]", ShouldPickUpNearby()));
			if(held_flashlight){
				var empty_slot= GetEmptySlot();
				if(empty_slot != -1){
					var str= "Put flashlight in inventory: tap [ ";
					str += empty_slot;
					str += " ]";
					display_text.push(new DisplayLine(str, false));
				}
			} else {
				var flashlight_slot= GetFlashlightSlot();
				if(flashlight_slot != -1){
					var str = "Equip flashlight: tap [ ";
					str += flashlight_slot;
					str += " ]";
					display_text.push(new DisplayLine(str, true));
				}
			}
			if(gun_instance){
				display_text.push(new DisplayLine("Fire weapon: tap [ left mouse button ]", false));
				var should_aim= (aim_spring.state < 0.5f);			
				display_text.push(new DisplayLine("Aim weapon: hold [ right mouse button ]", should_aim));
				display_text.push(new DisplayLine("Aim weapon: tap [ q ]", should_aim));
				display_text.push(new DisplayLine("Holster weapon: tap [ ~ ]", ShouldHolsterGun()));
			} else {
				display_text.push(new DisplayLine("Draw weapon: tap [ ~ ]", ShouldDrawWeapon()));
			}
			if(gun_instance){
				if(gun_script.HasSlide()){
					display_text.push(new DisplayLine("Pull back slide: hold [ r ]", gun_script.ShouldPullSlide()?true:false));
					display_text.push(new DisplayLine("Release slide lock: tap [ t ]", gun_script.ShouldReleaseSlideLock()?true:false));
				}
				if(gun_script.HasSafety()){
					display_text.push(new DisplayLine("Toggle safety: tap [ v ]", gun_script.IsSafetyOn()?true:false));
				}
				if(gun_script.HasAutoMod()){
					display_text.push(new DisplayLine("Toggle full-automatic: tap [ v ]", gun_script.ShouldToggleAutoMod()?true:false));
				}
				if(gun_script.HasHammer()){
					display_text.push(new DisplayLine("Pull back hammer: hold [ f ]", gun_script.ShouldPullBackHammer()?true:false));
				}
				if(gun_script.gun_type == GunType.REVOLVER){
					if(!gun_script.IsCylinderOpen()){
						display_text.push(new DisplayLine("Open cylinder: tap [ e ]", (gun_script.ShouldOpenCylinder() && loose_bullets.Count!=0)?true:false));
					} else {
						display_text.push(new DisplayLine("Close cylinder: tap [ r ]", (gun_script.ShouldCloseCylinder() || loose_bullets.Count==0)?true:false));
						display_text.push(new DisplayLine("Extract casings: hold [ v ]", gun_script.ShouldExtractCasings()?true:false));
						display_text.push(new DisplayLine("Insert bullet: tap [ z ]", (gun_script.ShouldInsertBullet() && loose_bullets.Count!=0)?true:false));
					}
					display_text.push(new DisplayLine("Spin cylinder: [ mousewheel ]", false));
				}
				if(mag_stage == HandMagStage.HOLD && !gun_script.IsThereAMagInGun()){
					var should_insert_mag= (magazine_instance_in_hand.GetComponent<mag_script>().NumRounds() >= 1);
					display_text.push(new DisplayLine("Insert magazine: tap [ z ]", should_insert_mag));
				} else if(mag_stage == HandMagStage.EMPTY && gun_script.IsThereAMagInGun()){
					display_text.push(new DisplayLine("Eject magazine: tap [ e ]", gun_script.ShouldEjectMag()?true:false));
				} else if(mag_stage == HandMagStage.EMPTY && !gun_script.IsThereAMagInGun()){
					var max_rounds_slot= GetMostLoadedMag();
					if(max_rounds_slot != -1){
						display_text.push(new DisplayLine("Equip magazine: tap [ "+max_rounds_slot+" ]", true));
					}
				}
			} else {
				if(CanLoadBulletsInMag()){
					display_text.push(new DisplayLine("Insert bullet in magazine: tap [ z ]", true));
				}
				if(CanRemoveBulletFromMag()){
					display_text.push(new DisplayLine("Remove bullet from magazine: tap [ r ]", false));
				}
			}
			if(mag_stage == HandMagStage.HOLD){
				var empty_slot = GetEmptySlot();
				if(empty_slot != -1){
					var str = "Put magazine in inventory: tap [ ";
					str += empty_slot;
					str += " ]";
					display_text.push(new DisplayLine(str, ShouldPutMagInInventory()));
				}
				display_text.push(new DisplayLine("Drop magazine: tap [ e ]", false));
			}
			
			display_text.push(new DisplayLine("", false));
			if(show_advanced_help){
				display_text.push(new DisplayLine("Advanced help:", false));
				display_text.push(new DisplayLine("Toggle crouch: [ c ]", false));
				if(aim_spring.state < 0.5f){
					display_text.push(new DisplayLine("Run: tap repeatedly [ w ]", false));
				}
				if(gun_instance){
					if(!gun_script.IsSafetyOn() && gun_script.IsHammerCocked()){
					display_text.push(new DisplayLine("Decock: Hold [f], hold [LMB], release [f]", ShouldPickUpNearby()));
					}
					if(!gun_script.IsSlideLocked() && !gun_script.IsSafetyOn()){
						display_text.push(new DisplayLine("Inspect chamber: hold [ t ] and then [ r ]", false));
					}
					if(mag_stage == HandMagStage.EMPTY && !gun_script.IsThereAMagInGun()){
						var max_rounds_slot = GetMostLoadedMag();
						if(max_rounds_slot != -1){
							display_text.push(new DisplayLine("Quick load magazine: double tap [ "+max_rounds_slot+" ]", false));
						}
					}
				}
				display_text.push(new DisplayLine("Reset game: hold [ l ]", false));
			} else {
				display_text.push(new DisplayLine("Advanced help: Hold [ ? ]", false));
			}
		}
		GUIStyle style = holder.gui_skin.label;
		var width= Screen.width * 0.5f;
		var offset= 0;
		foreach(DisplayLine line in display_text){
			if(line.bold){
				style.fontStyle = FontStyle.Bold;
			} else {
				style.fontStyle = FontStyle.Normal;
			}
			style.fontSize = 18;
			style.normal.textColor = new Color(0,0,0);
			GUI.Label( new Rect(width+0.5f,offset+0.5f,width+0.5f,offset+20+0.5f),line.str, style);
			if(line.bold){
				style.normal.textColor = new Color(1,1,1);
			} else {
				style.normal.textColor = new Color(0.7f,0.7f,0.7f);
			}
			GUI.Label( new Rect(width,offset,width,offset+30),line.str, style);
			offset += 20;
		}
		if(dead_fade > 0.0f){
			if(!texture_death_screen){
				Debug.LogError("Assign a Texture in the inspector.");
				return;
			}
			GUI.color = new Color(0,0,0,dead_fade);
			GUI.DrawTexture( new Rect(0,0,Screen.width,Screen.height), texture_death_screen, ScaleMode.StretchToFill, true);
		}
		if(win_fade > 0.0f){
			GUI.color = new Color(1,1,1,win_fade);
			GUI.DrawTexture( new Rect(0,0,Screen.width,Screen.height), texture_death_screen, ScaleMode.StretchToFill, true);
		}
	}
}