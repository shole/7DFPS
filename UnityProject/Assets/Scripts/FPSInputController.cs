using UnityEngine;
using System.Collections;

// Require a character controller to be attached to the same game object
[RequireComponent (typeof(CharacterMotor))]
[AddComponentMenu ("Character/FPS Input Controller")]
public class FPSInputController : MonoBehaviour {
	private CharacterMotor motor;

	private float forward_input_delay= 10.0f;
	private float old_vert_axis= 0.0f;
	private bool running= false;

	// Use this for initialization
	void  Awake (){
		motor = GetComponent<CharacterMotor>();
	}

	// Update is called once per frame
	void  Update (){
		// Get the input vector from kayboard or analog stick
        Vector3 directionVector;
        var leftHand = SixenseInput.GetController(SixenseHands.LEFT);
        bool hydraJump = false;
        if (leftHand != null && leftHand.Enabled)
        {
            directionVector = new Vector3(leftHand.JoystickX, 0, leftHand.JoystickY);
            hydraJump = leftHand.GetButton(SixenseButtons.BUMPER);
        }
        else
        {
            directionVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        }
		
		if(old_vert_axis < 0.9f && Input.GetAxis("Vertical") >= 0.9f){
			if(forward_input_delay < 0.4f && !GetComponent<AimScript>().IsAiming()){
				motor.SetRunning(Mathf.Clamp((0.4f-forward_input_delay)/0.2f,0.01f,1.0f));
				running = true;			
			}
			forward_input_delay = 0.0f;
		}
		forward_input_delay += Time.deltaTime;
		if(forward_input_delay > 0.4f || GetComponent<AimScript>().IsAiming()){
			motor.SetRunning(0.0f);
			running = false;
		}
		if(running){
			directionVector.z = 1.0f;
		}
		old_vert_axis = Input.GetAxis("Vertical");
		
		if (directionVector != Vector3.zero) {
			// Get the length of the directon vector and then normalize it
			// Dividing by the length is cheaper than normalizing when we already have the length anyway
			var directionLength= directionVector.magnitude;
			directionVector = directionVector / directionLength;
			
			// Make sure the length is no bigger than 1
			directionLength = Mathf.Min(1, directionLength);
			
			// Make the input vector more sensitive towards the extremes and less sensitive in the middle
			// This makes it easier to control slow speeds when using analog sticks
			directionLength = directionLength * directionLength;
			
			// Multiply the normalized direction vector by the modified length
			directionVector = directionVector * directionLength;
		}
		
		// Apply the direction to the CharacterMotor
		motor.inputMoveDirection = transform.rotation * directionVector;
		motor.inputJump = Input.GetButton("Jump") || hydraJump;	
	}
}