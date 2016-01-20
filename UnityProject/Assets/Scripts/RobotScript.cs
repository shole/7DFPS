using UnityEngine;
using System.Collections;

public class RobotScript : MonoBehaviour {
	public AudioClip[] sound_gunshot;
	public AudioClip[] sound_damage_camera;
	public AudioClip[] sound_damage_gun;
	public AudioClip[] sound_damage_battery;
	public AudioClip[] sound_damage_ammo;
	public AudioClip[] sound_damage_motor;
	public AudioClip[] sound_bump;
	public AudioClip sound_alert;
	public AudioClip sound_unalert;
	public AudioClip sound_engine_loop;
	public AudioClip sound_damaged_engine_loop;

	private AudioSource audiosource_taser;
	private AudioSource audiosource_motor;
	private GameObject object_audiosource_motor;
	private AudioSource audiosource_effect;
	private AudioSource audiosource_foley;
	private float sound_line_of_sight= 0.0f;

	public GameObject electric_spark_obj;
	public GameObject muzzle_flash;
	public GameObject bullet_obj;

	public enum RobotType {SHOCK_DRONE, STATIONARY_TURRET, MOBILE_TURRET, GUN_DRONE};
	public RobotType robot_type;

	private float gun_delay= 0.0f;
	private bool alive= true;
	private Spring rotation_x= new Spring(0.0f,0.0f,100.0f,0.0001f);
	private Spring rotation_y= new Spring(0.0f,0.0f,100.0f,0.0001f);
	private Quaternion initial_turret_orientation;
	private Vector3 initial_turret_position;
	private Transform gun_pivot;
	enum AIState {IDLE, ALERT, ALERT_COOLDOWN, AIMING, FIRING, DEACTIVATING, DEAD};
	private AIState ai_state = AIState.IDLE;
	private bool battery_alive= true;
	private bool motor_alive= true;
	private bool camera_alive= true;
	private bool trigger_alive= true;
	private bool barrel_alive= true;
	private bool ammo_alive= true;
	private bool trigger_down= false;
	private int bullets= 15;
	private const float kAlertDelay= 0.6f;
	private const float kAlertCooldownDelay= 2.0f;
	private float alert_delay= 0.0f;
	private float alert_cooldown_delay= 0.0f;
	private const float kMaxRange= 20.0f;
	private float rotor_speed= 0.0f;
	private float top_rotor_rotation= 0.0f;
	private float bottom_rotor_rotation= 0.0f;
	private Vector3 initial_pos;
	private bool stuck= false;
	private float stuck_delay= 0.0f;
	private Vector3 tilt_correction;
	private bool distance_sleep= false;
	private const float kSleepDistance= 20.0f;

	public Vector3 target_pos;
	public enum CameraPivotState {DOWN, WAIT_UP, UP, WAIT_DOWN};
	public CameraPivotState camera_pivot_state= CameraPivotState.WAIT_DOWN;
	public float camera_pivot_delay= 0.0f;
	public float camera_pivot_angle= 0.0f;

	private GameObject player;
	private Rigidbody _rigidbody;

	void  PlaySoundFromGroup ( AudioClip[] group ,   float volume  ){
		if(group.Length == 0){
			return;
		}
		var which_shot= Random.Range(0,group.Length);
		audiosource_effect.PlayOneShot(group[which_shot], volume * PlayerPrefs.GetFloat("sound_volume", 1.0f));
	}

	GameObject GetTurretLightObject (){
		return transform.FindChild("gun pivot").FindChild("camera").FindChild("light").gameObject;
	}

	GameObject GetDroneLightObject (){
		return transform.FindChild("camera_pivot").FindChild("camera").FindChild("light").gameObject;
	}

	GameObject GetDroneLensFlareObject (){
		return transform.FindChild("camera_pivot").FindChild("camera").FindChild("lens flare").gameObject;
	}


	Quaternion RandomOrientation (){
		return Quaternion.Euler(Random.Range(0,360),Random.Range(0,360),Random.Range(0,360));
	}

	void  Damage ( GameObject obj  ){
		var damage_done= false;
		if(obj.name == "battery" && battery_alive){
			battery_alive = false;
			motor_alive = false;
			camera_alive = false;
			trigger_alive = false;
			if(robot_type == RobotType.SHOCK_DRONE){
				barrel_alive = false;
			}
			PlaySoundFromGroup(sound_damage_battery,1.0f);
			rotation_x.target_state = 40.0f;
			damage_done = true;
		} else if((obj.name == "pivot motor" || obj.name == "motor") && motor_alive){
			motor_alive = false;
			PlaySoundFromGroup(sound_damage_motor,1.0f);
			damage_done = true;
		} else if(obj.name == "power cable" && (camera_alive || trigger_alive)){
			camera_alive = false;
			damage_done = true;
			PlaySoundFromGroup(sound_damage_battery,1.0f);
			trigger_alive = false;
		} else if(obj.name == "ammo box" && ammo_alive){
			ammo_alive = false;
			PlaySoundFromGroup(sound_damage_ammo,1.0f);
			damage_done = true;
		} else if((obj.name == "gun" || obj.name == "shock prod") && barrel_alive){
			barrel_alive = false;
			PlaySoundFromGroup(sound_damage_gun,1.0f);
			damage_done = true;
		} else if(obj.name == "camera" && camera_alive){
			camera_alive = false;
			PlaySoundFromGroup(sound_damage_camera,1.0f);
			damage_done = true;
		} else if(obj.name == "camera armor" && camera_alive){
			camera_alive = false;
			PlaySoundFromGroup(sound_damage_camera,1.0f);
			damage_done = true;
		}
		if(damage_done){
			Instantiate(electric_spark_obj, obj.transform.position, RandomOrientation());
		}
	}

	public void  WasShotInternal ( GameObject obj  ){
		Damage(obj);
	}

	public void  WasShot ( GameObject obj ,   Vector3 pos ,   Vector3 vel  ){
		if(transform.parent && transform.parent.gameObject.name == "gun pivot"){
			var x_axis= transform.FindChild("point_pivot").rotation * new Vector3(1,0,0);
			var y_axis= transform.FindChild("point_pivot").rotation * new Vector3(0,1,0);
			var z_axis= transform.FindChild("point_pivot").rotation * new Vector3(0,0,1);
			
			var y_plane_vel= new Vector3(Vector3.Dot(vel, x_axis), 0.0f, Vector3.Dot(vel, z_axis));
			var rel_pos= pos - transform.FindChild("point_pivot").position;
			var y_plane_pos= new Vector3(Vector3.Dot(rel_pos, z_axis), 0.0f, -Vector3.Dot(rel_pos, x_axis));
			rotation_y.vel += Vector3.Dot(y_plane_vel, y_plane_pos) * 10.0f;
			
			var x_plane_vel= new Vector3(Vector3.Dot(vel, y_axis), 0.0f, Vector3.Dot(vel, z_axis));
			rel_pos = pos - transform.FindChild("point_pivot").position;
			var x_plane_pos= new Vector3(-Vector3.Dot(rel_pos, z_axis), 0.0f, Vector3.Dot(rel_pos, y_axis));
			rotation_x.vel += Vector3.Dot(x_plane_vel, x_plane_pos) * 10.0f;
		}
		if(robot_type == RobotType.SHOCK_DRONE){
			if(Random.Range(0.0f,1.0f) < 0.5f){
				Damage(transform.FindChild("battery").gameObject);
			}
		} else {
			if(Random.Range(0.0f,1.0f) < 0.25f){
				Damage(transform.FindChild("battery").gameObject);
			}
		}
		Damage(obj);
	}

	void  Start (){
		
		audiosource_effect = gameObject.AddComponent<AudioSource>();
		audiosource_effect.rolloffMode = AudioRolloffMode.Linear;
		audiosource_effect.maxDistance = 30;

		object_audiosource_motor = new GameObject("motor audiosource object");
		object_audiosource_motor.transform.parent = transform;
		object_audiosource_motor.transform.localPosition = new Vector3(0,0,0);
		
		audiosource_motor = object_audiosource_motor.AddComponent<AudioSource>();
		object_audiosource_motor.AddComponent<AudioLowPassFilter>();
		audiosource_motor.loop = true;
		audiosource_motor.volume = 0.4f * PlayerPrefs.GetFloat("sound_volume", 1.0f);
		audiosource_motor.clip = sound_engine_loop;
		
		switch(robot_type){
			case RobotType.STATIONARY_TURRET:
				gun_pivot = transform.FindChild("gun pivot");
				initial_turret_orientation = gun_pivot.transform.localRotation;
				initial_turret_position = gun_pivot.transform.localPosition;
				audiosource_motor.rolloffMode = AudioRolloffMode.Linear;
				audiosource_motor.maxDistance = 4;
				break;
			case RobotType.SHOCK_DRONE:
				audiosource_motor.maxDistance = 8;
				audiosource_foley = gameObject.AddComponent<AudioSource>();
				audiosource_taser = gameObject.AddComponent<AudioSource>();
				audiosource_taser.rolloffMode = AudioRolloffMode.Linear;
				audiosource_taser.loop = true;
				audiosource_taser.clip = sound_gunshot[0];
				break;
		}
		
		initial_pos = transform.position;	
		target_pos = initial_pos;
		
		_rigidbody = GetComponent<Rigidbody>();
	}

	void  UpdateStationaryTurret (){
		if(Vector3.Distance(player.transform.position, transform.position) > kSleepDistance){
			GetTurretLightObject().GetComponent<Light>().shadows = LightShadows.None;		
			if(audiosource_motor.isPlaying){
				audiosource_motor.Stop();
			}
			return;
		} else {
			if(!audiosource_motor.isPlaying){
				audiosource_motor.volume = PlayerPrefs.GetFloat("sound_volume", 1.0f);
				audiosource_motor.Play();
			}
			audiosource_motor.volume = 0.4f * PlayerPrefs.GetFloat("sound_volume", 1.0f);
			if(GetTurretLightObject().GetComponent<Light>().intensity > 0.0f){
				GetTurretLightObject().GetComponent<Light>().shadows = LightShadows.Hard;
			} else {
				GetTurretLightObject().GetComponent<Light>().shadows = LightShadows.None;
			}
		}
		if(motor_alive){
			switch(ai_state){
				case AIState.IDLE:
					rotation_y.target_state += Time.deltaTime * 100.0f;
					break;				
				case AIState.AIMING:
				case AIState.ALERT:
				case AIState.ALERT_COOLDOWN:
				case AIState.FIRING:
					var rel_pos= target_pos - transform.FindChild("point_pivot").position;
				var x_axis= transform.FindChild("point_pivot").rotation * new Vector3(1,0,0);
				var y_axis= transform.FindChild("point_pivot").rotation * new Vector3(0,1,0);
				var z_axis= transform.FindChild("point_pivot").rotation * new Vector3(0,0,1);
				var y_plane_pos= new Vector3(Vector3.Dot(rel_pos, z_axis), 0.0f, -Vector3.Dot(rel_pos, x_axis)).normalized;
					var target_y= Mathf.Atan2(y_plane_pos.x, y_plane_pos.z)/Mathf.PI*180-90;
					while(target_y > rotation_y.state + 180){
						rotation_y.state += 360.0f;
					}
					while(target_y < rotation_y.state - 180){
						rotation_y.state -= 360.0f;
					}
					rotation_y.target_state = target_y;
					var y_height= Vector3.Dot(y_axis, rel_pos.normalized);
					var target_x= -Mathf.Asin(y_height)/Mathf.PI*180;
					rotation_x.target_state = target_x;
					rotation_x.target_state = Mathf.Min(40,Mathf.Max(-40,target_x));
					break;
			}
		}
		if(battery_alive){
			switch(ai_state){
				case AIState.FIRING:
					trigger_down = true;
					break;
				default:
					trigger_down = false;
					break;
			}
		}
		if(barrel_alive){
			if(trigger_down){
				if(gun_delay <= 0.0f){
					gun_delay += 0.1f;
					var point_muzzle_flash= gun_pivot.FindChild("gun").FindChild("point_muzzleflash");
					Instantiate(muzzle_flash, point_muzzle_flash.position, point_muzzle_flash.rotation);
					PlaySoundFromGroup(sound_gunshot, 1.0f);
					
					var bullet= Instantiate(bullet_obj, point_muzzle_flash.position, point_muzzle_flash.rotation) as GameObject;
					bullet.GetComponent<BulletScript>().SetVelocity(point_muzzle_flash.forward * 300.0f);
					bullet.GetComponent<BulletScript>().SetHostile();				
					rotation_x.vel += Random.Range(-50,50);
					rotation_y.vel += Random.Range(-50,50);
					--bullets;
				}
			}
			if(ammo_alive && bullets > 0){
				gun_delay = Mathf.Max(0.0f, gun_delay - Time.deltaTime);
			}
		}
		var danger= 0.0f;
		var dist= Vector3.Distance(player.transform.position, transform.position);
		if(battery_alive){
			danger += Mathf.Max(0.0f, 1.0f - dist/kMaxRange);
		}
		if(camera_alive){
			if(danger > 0.0f){
				danger = Mathf.Min(0.2f, danger);
			}
			if(ai_state == AIState.AIMING || ai_state == AIState.FIRING){
				danger = 1.0f;
			}
			if(ai_state == AIState.ALERT || ai_state == AIState.ALERT_COOLDOWN){
				danger += 0.5f;
			}
			
			var camera= transform.FindChild("gun pivot").FindChild("camera");
			var rel_pos = player.transform.position - camera.position;
			var sees_target= false;
			if(dist < kMaxRange && Vector3.Dot(camera.rotation*new Vector3(0,-1,0), rel_pos.normalized) > 0.7f){
				RaycastHit hit;
				if(!Physics.Linecast(camera.position, player.transform.position, out hit, 1<<0)){
					sees_target = true;
				}
			}
			if(sees_target){
				switch(ai_state){
					case AIState.IDLE:
						ai_state = AIState.ALERT;
						audiosource_effect.PlayOneShot(sound_alert, 0.3f * PlayerPrefs.GetFloat("sound_volume", 1.0f));
						alert_delay = kAlertDelay;
						break;
					case AIState.AIMING:
					if(Vector3.Dot(camera.rotation*new Vector3(0,-1,0), rel_pos.normalized) > 0.9f){
							ai_state = AIState.FIRING;
						}
						target_pos = player.transform.position;
						break;					
					case AIState.FIRING:
						target_pos = player.transform.position;
						break;
					case AIState.ALERT:
						alert_delay -= Time.deltaTime;
						if(alert_delay <= 0.0f){
							ai_state = AIState.AIMING;
						}
						target_pos = player.transform.position;
						break;
					case AIState.ALERT_COOLDOWN:
						ai_state = AIState.ALERT;
						alert_delay = kAlertDelay;
						break;
				}
			} else {
				switch(ai_state){
					case AIState.AIMING:
					case AIState.FIRING:
					case AIState.ALERT:
						ai_state = AIState.ALERT_COOLDOWN;
						alert_cooldown_delay = kAlertCooldownDelay;
						break;
					case AIState.ALERT_COOLDOWN:
						alert_cooldown_delay -= Time.deltaTime;
						if(alert_cooldown_delay <= 0.0f){
							ai_state = AIState.IDLE;
							audiosource_effect.PlayOneShot(sound_unalert, 0.3f * PlayerPrefs.GetFloat("sound_volume", 1.0f));
						}
						break;
				}
			}
			switch(ai_state){
				case AIState.IDLE:
				GetTurretLightObject().GetComponent<Light>().color = new Color(0,0,1);
					break;
				case AIState.AIMING:
				GetTurretLightObject().GetComponent<Light>().color = new Color(1,0,0);
					break;
				case AIState.ALERT:
				case AIState.ALERT_COOLDOWN:
				GetTurretLightObject().GetComponent<Light>().color = new Color(1,1,0);
					break;
			}
		}
		player.GetComponent<MusicScript>().AddDangerLevel(danger);
		if(!camera_alive){
			GetTurretLightObject().GetComponent<Light>().intensity *= Mathf.Pow(0.01f, Time.deltaTime);
		}
		var target_pitch= (Mathf.Abs(rotation_y.vel) + Mathf.Abs(rotation_x.vel)) * 0.01f;
		target_pitch = Mathf.Clamp(target_pitch, 0.2f, 2.0f);
		audiosource_motor.pitch = Mathf.Lerp(audiosource_motor.pitch, target_pitch, Mathf.Pow(0.0001f, Time.deltaTime));
		
		rotation_x.Update();
		rotation_y.Update();
		gun_pivot.localRotation = initial_turret_orientation;
		gun_pivot.localPosition = initial_turret_position;
		gun_pivot.RotateAround(
			transform.FindChild("point_pivot").position, 
			transform.FindChild("point_pivot").rotation * new Vector3(1,0,0),
			rotation_x.state);
		gun_pivot.RotateAround(
			transform.FindChild("point_pivot").position, 
			transform.FindChild("point_pivot").rotation * new Vector3(0,1,0),
			rotation_y.state);
	}

	void  UpdateDrone (){
		if(Vector3.Distance(player.transform.position, transform.position) > kSleepDistance){
			GetDroneLightObject().GetComponent<Light>().shadows = LightShadows.None;
			if(motor_alive){
				distance_sleep = true;
				_rigidbody.Sleep();
			}
			if(audiosource_motor.isPlaying){
				audiosource_motor.Stop();
			}
			return;
		} else {
			if(GetDroneLightObject().GetComponent<Light>().intensity > 0.0f){
				GetDroneLightObject().GetComponent<Light>().shadows = LightShadows.Hard;
			} else {
				GetDroneLightObject().GetComponent<Light>().shadows = LightShadows.None;
			}
			if(motor_alive && distance_sleep){
				_rigidbody.WakeUp();
				distance_sleep = false;
			}
			if(!audiosource_motor.isPlaying){
				audiosource_motor.volume = PlayerPrefs.GetFloat("sound_volume", 1.0f);
				audiosource_motor.Play();
			}
		}
		var rel_pos= target_pos - transform.position;
		if(motor_alive){		
			var kFlyDeadZone= 0.2f;
			var kFlySpeed= 10.0f;
			var target_vel= (target_pos - transform.position) / kFlyDeadZone;
			if(target_vel.magnitude > 1.0f){
				target_vel = target_vel.normalized;
			}
			target_vel *= kFlySpeed;
			var target_accel= (target_vel - _rigidbody.velocity);
			if(ai_state == AIState.IDLE){
				target_accel *= 0.1f;
			}
			target_accel.y += 9.81f;
			
			rotor_speed = target_accel.magnitude;
			rotor_speed = Mathf.Clamp(rotor_speed, 0.0f, 14.0f);
			
			var up= transform.rotation * new Vector3(0,1,0);
			Quaternion correction = Quaternion.FromToRotation(up, target_accel.normalized);
			Vector3 correction_vec;
			float correction_angle;
			correction.ToAngleAxis(out correction_angle, out correction_vec);
			tilt_correction = correction_vec * correction_angle;
			tilt_correction -= _rigidbody.angularVelocity;
			
			
			var x_axis= transform.rotation * new Vector3(1,0,0);
			var y_axis= transform.rotation * new Vector3(0,1,0);
			var z_axis= transform.rotation * new Vector3(0,0,1);
			if(ai_state != AIState.IDLE){
				var y_plane_pos= new Vector3(Vector3.Dot(rel_pos, z_axis), 0.0f, -Vector3.Dot(rel_pos, x_axis)).normalized;
				var target_y= Mathf.Atan2(y_plane_pos.x, y_plane_pos.z)/Mathf.PI*180-90;
				while(target_y > 180){
					target_y -= 360.0f;
				}
				while(target_y < -180){
					target_y += 360.0f;
				}
				tilt_correction += y_axis * target_y;	
				tilt_correction *= 5.0f;
			} else {
				tilt_correction += y_axis;	
			}
			
			if(ai_state == AIState.IDLE){
				tilt_correction *= 0.1f;
			}
			
			if(_rigidbody.velocity.magnitude < 0.2f){ 
				stuck_delay += Time.deltaTime;
				if(stuck_delay > 1.0f){
					target_pos = transform.position + new Vector3(Random.Range(-1.0f,1.0f), Random.Range(-1.0f,1.0f), Random.Range(-1.0f,1.0f));
					stuck_delay = 0.0f;
				}
			} else {
				stuck_delay = 0.0f;
			}
			
		} else {
			rotor_speed = Mathf.Max(0.0f, rotor_speed - Time.deltaTime * 5.0f);
			_rigidbody.angularDrag = 0.05f;
		}
		if(barrel_alive && ai_state == AIState.FIRING){
			if(!audiosource_taser.isPlaying){
				audiosource_taser.volume = PlayerPrefs.GetFloat("sound_volume", 1.0f);
				audiosource_taser.Play();
			} else {
				audiosource_taser.volume = PlayerPrefs.GetFloat("sound_volume", 1.0f);
			}
			if(gun_delay <= 0.0f){
				gun_delay = 0.1f;	
				Instantiate(muzzle_flash, transform.FindChild("point_spark").position, RandomOrientation());
				if(Vector3.Distance(transform.FindChild("point_spark").position, player.transform.position) < 1){;
					player.GetComponent<AimScript>().Shock();
				}
			}
		} else {
			audiosource_taser.Stop();
		}
		gun_delay = Mathf.Max(0.0f, gun_delay - Time.deltaTime);
		
		top_rotor_rotation += rotor_speed * Time.deltaTime * 1000.0f;
		bottom_rotor_rotation -= rotor_speed * Time.deltaTime * 1000.0f;
		var bottom_rotor = transform.FindChild ("bottom rotor");
		var top_rotor = transform.FindChild ("top rotor");
		if(rotor_speed * Time.timeScale > 7.0f){
			bottom_rotor.gameObject.GetComponent<Renderer>().enabled = false;
			top_rotor.gameObject.GetComponent<Renderer>().enabled = false;
		} else {
			bottom_rotor.gameObject.GetComponent<Renderer>().enabled = true;
			top_rotor.gameObject.GetComponent<Renderer>().enabled = true;
		}
		var bottomEuler = bottom_rotor.localEulerAngles;
		bottomEuler.y = bottom_rotor_rotation;
		bottom_rotor.localEulerAngles = bottomEuler;
		var topEuler = top_rotor.localEulerAngles;
		topEuler.y = top_rotor_rotation;
		top_rotor.localEulerAngles = topEuler;
		
		//rigidbody.velocity += transform.rotation * Vector3(0,1,0) * rotor_speed * Time.deltaTime;
		if(camera_alive){
			if(ai_state == AIState.IDLE){
				switch(camera_pivot_state) {
					case CameraPivotState.DOWN:
						camera_pivot_angle += Time.deltaTime * 25.0f;
						if(camera_pivot_angle > 50){
							camera_pivot_angle = 50;
							camera_pivot_state = CameraPivotState.WAIT_UP;
							camera_pivot_delay = 0.2f;
						}
						break;
					case CameraPivotState.UP:
						camera_pivot_angle -= Time.deltaTime * 25.0f;
						if(camera_pivot_angle < 0){
							camera_pivot_angle = 0;
							camera_pivot_state = CameraPivotState.WAIT_DOWN;
							camera_pivot_delay = 0.2f;
						}
						break;
					case CameraPivotState.WAIT_DOWN:
						camera_pivot_delay -= Time.deltaTime;
						if(camera_pivot_delay < 0){
							camera_pivot_state = CameraPivotState.DOWN;
						}
						break;
					case CameraPivotState.WAIT_UP:
						camera_pivot_delay -= Time.deltaTime;
						if(camera_pivot_delay < 0){
							camera_pivot_state = CameraPivotState.UP;
						}
						break;
				}
			} else {
				camera_pivot_angle -= Time.deltaTime * 25.0f;
				if(camera_pivot_angle < 0){
					camera_pivot_angle = 0;
				}
			}
			var cam_pivot= transform.FindChild("camera_pivot");
			var cam_pivotEuler= cam_pivot.localEulerAngles;
			cam_pivotEuler.x = camera_pivot_angle;
			cam_pivot.localEulerAngles = cam_pivotEuler;
			var dist= Vector3.Distance(player.transform.position, transform.position);
			var danger= Mathf.Max(0.0f, 1.0f - dist/kMaxRange);
			if(danger > 0.0f){
				danger = Mathf.Min(0.2f, danger);
			}
			if(ai_state == AIState.AIMING || ai_state == AIState.FIRING){
				danger = 1.0f;
			}
			if(ai_state == AIState.ALERT || ai_state == AIState.ALERT_COOLDOWN){
				danger += 0.5f;
			}
			player.GetComponent<MusicScript>().AddDangerLevel(danger);
			
			var camera= transform.FindChild("camera_pivot").FindChild("camera");
			rel_pos = player.transform.position - camera.position;
			var sees_target= false;
			if(dist < kMaxRange && Vector3.Dot(camera.rotation*new Vector3(0,-1,0), rel_pos.normalized) > 0.7f){
				RaycastHit hit;
				if(!Physics.Linecast(camera.position, player.transform.position, out hit, 1<<0)){
					sees_target = true;
				}
			}
			if(sees_target){
				var new_target= player.transform.position + player.GetComponent<CharacterMotor>().GetVelocity() * 
								Mathf.Clamp(Vector3.Distance(player.transform.position, transform.position) * 0.1f, 0.5f, 1.0f);
				switch(ai_state){
					case AIState.IDLE:
						ai_state = AIState.ALERT;
						alert_delay = kAlertDelay;
						audiosource_effect.PlayOneShot(sound_alert, 0.3f * PlayerPrefs.GetFloat("sound_volume", 1.0f));
						break;
					case AIState.AIMING:
						target_pos = new_target;
						if(Vector3.Distance(transform.position, target_pos) < 4){
							ai_state = AIState.FIRING;
						}
						target_pos.y += 1.0f;
						break;					
					case AIState.FIRING:
						target_pos = new_target;
						if(Vector3.Distance(transform.position, target_pos) > 4){
							ai_state = AIState.AIMING;
						}
						break;
					case AIState.ALERT:
						alert_delay -= Time.deltaTime;
						target_pos = new_target;
						target_pos.y += 1.0f;
						if(alert_delay <= 0.0f){
							ai_state = AIState.AIMING;
						}
						break;
					case AIState.ALERT_COOLDOWN:
						ai_state = AIState.ALERT;
						alert_delay = kAlertDelay;
						break;
				}
			} else {
				switch(ai_state){
					case AIState.AIMING:
					case AIState.FIRING:
					case AIState.ALERT:
						ai_state = AIState.ALERT_COOLDOWN;
						alert_cooldown_delay = kAlertCooldownDelay;
						break;
					case AIState.ALERT_COOLDOWN:
						alert_cooldown_delay -= Time.deltaTime;
						if(alert_cooldown_delay <= 0.0f){
							ai_state = AIState.IDLE;
							audiosource_effect.PlayOneShot(sound_unalert, 0.3f * PlayerPrefs.GetFloat("sound_volume", 1.0f));
						}
						break;
				}
			}
			switch(ai_state){
				case AIState.IDLE:
					GetDroneLightObject().GetComponent<Light>().color = new Color(0,0,1);
					break;
				case AIState.AIMING:
					GetDroneLightObject().GetComponent<Light>().color = new Color(1,0,0);
					break;
				case AIState.ALERT:
				case AIState.ALERT_COOLDOWN:
					GetDroneLightObject().GetComponent<Light>().color = new Color(1,1,0);
					break;
			}
		}
		if(!camera_alive){
			GetDroneLightObject().GetComponent<Light>().intensity *= Mathf.Pow(0.01f, Time.deltaTime);
		}
		(GetDroneLensFlareObject().GetComponent<LensFlare>() as LensFlare).color = GetDroneLightObject().GetComponent<Light>().color;
		(GetDroneLensFlareObject().GetComponent<LensFlare>() as LensFlare).brightness = GetDroneLightObject().GetComponent<Light>().intensity;
		var target_pitch= rotor_speed * 0.2f;
		target_pitch = Mathf.Clamp(target_pitch, 0.2f, 3.0f);
		audiosource_motor.pitch = Mathf.Lerp(audiosource_motor.pitch, target_pitch, Mathf.Pow(0.0001f, Time.deltaTime));
		audiosource_motor.volume = rotor_speed * 0.1f * PlayerPrefs.GetFloat("sound_volume", 1.0f);

		audiosource_motor.volume -= Vector3.Distance(GameObject.Find("Camera (head)").transform.position, transform.position) * 0.0125f * PlayerPrefs.GetFloat("sound_volume", 1.0f);

		var line_of_sight= true;
		{
			RaycastHit hit;
			if (Physics.Linecast (transform.position, GameObject.Find ("Camera (head)").transform.position, out hit, 1 << 0)) {
				line_of_sight = false;
			}
		}
		if(line_of_sight){
			sound_line_of_sight += Time.deltaTime * 3.0f;
		} else {
			sound_line_of_sight -= Time.deltaTime * 3.0f;
		}
		sound_line_of_sight = Mathf.Clamp(sound_line_of_sight,0,1);
		
		audiosource_motor.volume *= 0.5f + sound_line_of_sight * 0.5f;
		object_audiosource_motor.GetComponent<AudioLowPassFilter>().cutoffFrequency = 
			Mathf.Lerp(5000, 44000, sound_line_of_sight);
	}


	void  Update (){
		if (player == null)
		{
			player = GameObject.Find("Player");
		}
		switch(robot_type){
			case RobotType.STATIONARY_TURRET:
				UpdateStationaryTurret();
				break;
			case RobotType.SHOCK_DRONE:
				UpdateDrone();
				break;
		}
	}

	void  OnCollisionEnter ( Collision collision  ){
		if(robot_type == RobotType.SHOCK_DRONE){
			if(collision.impactForceSum.magnitude > 10){
				if(Random.Range(0.0f,1.0f)<0.5f && motor_alive){
					Damage(transform.FindChild("motor").gameObject);
				} else if(Random.Range(0.0f,1.0f)<0.5f && camera_alive){
					Damage(transform.FindChild("camera_pivot").FindChild("camera").gameObject);
				} else if(Random.Range(0.0f,1.0f)<0.5f && battery_alive){
					Damage(transform.FindChild("battery").gameObject);
				} else {
					motor_alive = true;
					Damage(transform.FindChild("motor").gameObject);
				} 
			} else {
				var which_shot= Random.Range(0,sound_bump.Length);
				audiosource_foley.PlayOneShot(sound_bump[which_shot], collision.impactForceSum.magnitude * 0.15f * PlayerPrefs.GetFloat("sound_volume", 1.0f));
			}
		}
	}

	void  FixedUpdate (){
		if(robot_type == RobotType.SHOCK_DRONE && !distance_sleep){
			_rigidbody.AddForce(transform.rotation * new Vector3(0,1,0) * rotor_speed, ForceMode.Force);
			if(motor_alive){
				_rigidbody.AddTorque(tilt_correction, ForceMode.Force);
			}
		}
	}
}