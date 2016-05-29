using UnityEngine;
using System.Collections;
using Leap;
using System;

public class Drawer : MonoBehaviour {

	public CanvasAutomation canvas;

	private const float THUMB_TRIGGER_DISTANCE = 0.04f;
	private const float MIN_CONFIDENCE = 0.2f;


	private float FIST_TRIGGER_DISTANCE = 0.145f;
	private float temp_DIST = 0f;

	void Update () {
		HandModel hand_model = GetComponent<HandModel>();
		Hand leap_hand = hand_model.GetLeapHand();

		if (leap_hand == null)
			return;

		bool draw_trigger = false;
		bool draw_fist = false;

		Vector3 thumb_tip = leap_hand.Fingers[0].TipPosition.ToUnityScaled();

		//draw if one finger is close to the thumb
		for (int i = 1; i < 5 && !draw_trigger; ++i) {
			for (int j = 0; j < 4 && !draw_trigger; ++j) {
				Finger.FingerJoint joint = (Finger.FingerJoint)(j);
				Vector3 difference = leap_hand.Fingers[i].JointPosition(joint).ToUnityScaled() - thumb_tip;
				if (difference.magnitude < THUMB_TRIGGER_DISTANCE && leap_hand.Confidence > MIN_CONFIDENCE) {
					draw_trigger = true;
				}
			}
		}

		//draw if you have a fist
		for (int k = 1; k < 5; k++) {
			Finger.FingerJoint joint = (Finger.FingerJoint)(k);
			Vector3 difference = leap_hand.Fingers[k].JointPosition(joint).ToUnityScaled() - thumb_tip;
			temp_DIST += difference.magnitude;
		}

		if (temp_DIST <= FIST_TRIGGER_DISTANCE) {
			draw_fist = true;
		}

		temp_DIST = 0f;

		//it checks if the distance between the thumb and other fingers is less than a threshold and if yes
		//then it allows to draw
		if (draw_trigger)
			canvas.AddNextPoint(hand_model.fingers[1].GetJointPosition(FingerModel.NUM_JOINTS - 1), draw_fist);
	}
}
