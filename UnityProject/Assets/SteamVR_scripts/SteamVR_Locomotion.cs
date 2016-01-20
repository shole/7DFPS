using UnityEngine;
using System.Collections;
using Valve.VR;

/*
public struct ClickedEventArgs {
    public uint controllerIndex;
    public uint flags;
    public float padX, padY;
}

public delegate void ClickedEventHandler(object sender, ClickedEventArgs e);

*/
public class SteamVR_Locomotion : MonoBehaviour {
    private Transform playerBody;
    private Transform playerHead;
    private Transform playerFeet;

    public float locomotionForce = 0.1f;
    public float deltaMagic = 3000f; // delta magic stolen from NewtonVR

    private Vector3 playerVector;


    void Start() {
        playerBody = transform.FindChild("PlayerBody").transform;
        playerFeet = transform.FindChild("[CameraRig]").transform;
        playerHead = playerFeet.FindChild("Camera (head)").transform;

        StartCoroutine(addTrackedControllers());
    }

    IEnumerator addTrackedControllers() { // continuously make sure we have tracked controllers in hand
        while (true) {
            yield return new WaitForSeconds(1f);
            SteamVR_TrackedObject[] trackedObjects = transform.GetComponentsInChildren<SteamVR_TrackedObject>();
            for (int i = 0; i < trackedObjects.Length; i++) {
                if (
                    trackedObjects[i].index != SteamVR_TrackedObject.EIndex.Hmd
                    &&
                    !trackedObjects[i].transform.GetComponent<SteamVR_TrackedController>()
                    ) {
                    Debug.Log("Added tracked controller for index " + i);
                    SteamVR_TrackedController trackedController =
                        trackedObjects[i].transform.gameObject.AddComponent<SteamVR_TrackedController>();
                    trackedController.controllerIndex = (uint)trackedObjects[i].index;

                    trackedController.PadTouched += new ClickedEventHandler(PadTouched);
                    trackedController.PadUntouched += new ClickedEventHandler(PadUntouched);
                    trackedController.PadUpdated += new ClickedEventHandler(PadTouched);
                }
            }
        }
    }

    void PadTouched(object sender, ClickedEventArgs e) {
        //Debug.Log(((SteamVR_TrackedController)sender).transform.rotation.eulerAngles.y);

        Vector3 controllerDirectionY = ((SteamVR_TrackedController)sender).transform.forward;
        controllerDirectionY.y = 0f; // 2d
        controllerDirectionY.Normalize();
        controllerDirectionY *= e.padY;

        Vector3 controllerDirectionX = ((SteamVR_TrackedController)sender).transform.right;
        controllerDirectionX.y = 0f;  // 2d
        controllerDirectionX.Normalize();
        controllerDirectionX *= e.padX;

        Vector3 controllerDirection = controllerDirectionX + controllerDirectionY;

        playerVector = controllerDirection * locomotionForce;
    }

    void PadUntouched(object sender, ClickedEventArgs e) {
        playerVector = Vector3.zero;
    }

    void FixedUpdate() {

        // move player body under player head
        Vector3 positionDiff = playerHead.position - playerBody.position;
        positionDiff.y = 0; // 2d
        float diffMagnitude = positionDiff.magnitude;
        positionDiff *= deltaMagic * Time.fixedDeltaTime;
        if (diffMagnitude < 0.1f) {
            positionDiff.y = playerBody.GetComponent<Rigidbody>().velocity.y; // only allow vertical (= falling) when body very near head, to prevent rubberbanding into the sky
        }
        playerBody.GetComponent<Rigidbody>().velocity = positionDiff; // body wants to be under player
        //Debug.Log(playerBody.GetComponent<Rigidbody>().velocity);

        playerFeet.localPosition += playerVector * locomotionForce * Time.fixedDeltaTime * (1f - diffMagnitude); // player locomotion by moving player's camerarig
    }

    void Update() {

        // update player height
        Vector3 playerBodyScale = playerBody.localScale;
        playerBodyScale.y = Mathf.Max(0.5f, playerHead.localPosition.y);
        playerBody.localScale = playerBodyScale;

        // update head vertical
        Vector3 playerFeetPos = playerFeet.localPosition;
        playerFeetPos.y = playerBody.localPosition.y;
        playerFeet.localPosition = playerFeetPos;
    }




}
