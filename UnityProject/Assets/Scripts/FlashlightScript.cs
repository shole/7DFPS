using UnityEngine;
using System.Collections;

public class FlashlightScript : MonoBehaviour {
	public AnimationCurve battery_curve;
	public AudioClip sound_turn_on;
	public AudioClip sound_turn_off;
	private float kSoundVolume= 0.3f;
	private bool switch_on= false;
	private const float max_battery_life= 60*60*5.5f;
	private float battery_life_remaining= max_battery_life;

	private float initial_pointlight_intensity;
	private float initial_spotlight_intensity;

	private Light Pointlight;
	private Light Spotlight;
	private Rigidbody _rigidbody;

	void  Awake (){
		switch_on = false;// Random.Range(0.0f,1.0f) < 0.5f;
	}

	void  Start (){
		_rigidbody = GetComponent<Rigidbody>();
		Pointlight = transform.FindChild("Pointlight").GetComponent<Light>();
		Spotlight = transform.FindChild("Spotlight").GetComponent<Light>();
		initial_pointlight_intensity = Pointlight.intensity;
		initial_spotlight_intensity = Spotlight.intensity;
		battery_life_remaining = Random.Range(max_battery_life*0.2f, max_battery_life);
	}

	public void  TurnOn (){
		if(!switch_on){
			if (_rigidbody)
			{
				Destroy(_rigidbody);
				_rigidbody = null;
			}
			switch_on = true;
			GetComponent<AudioSource>().PlayOneShot(sound_turn_on, kSoundVolume * PlayerPrefs.GetFloat("sound_volume", 1.0f));
		}
	}

	public void  TurnOff (){
		if(switch_on){
			switch_on = false;
			GetComponent<AudioSource>().PlayOneShot(sound_turn_off, kSoundVolume * PlayerPrefs.GetFloat("sound_volume", 1.0f));
		}
	}

	void  Update (){
		if(switch_on){
			battery_life_remaining -= Time.deltaTime;
			if(battery_life_remaining <= 0.0f){
				battery_life_remaining = 0.0f;
			}
			var battery_curve_eval= battery_curve.Evaluate(1.0f-battery_life_remaining/max_battery_life);
			Pointlight.intensity = initial_pointlight_intensity * battery_curve_eval * 8.0f;
			Spotlight.intensity = initial_spotlight_intensity * battery_curve_eval * 3.0f;
			Pointlight.enabled = true;
			Spotlight.enabled = true;
		} else {
			Pointlight.enabled = false;
			Spotlight.enabled = false;
		}
		if(_rigidbody){
			Pointlight.enabled = true;
			Pointlight.intensity = 1.0f + Mathf.Sin(Time.time * 2.0f);
			Pointlight.range = 1.0f;
		} else {
			Pointlight.range = 10.0f;
		}
	}
}