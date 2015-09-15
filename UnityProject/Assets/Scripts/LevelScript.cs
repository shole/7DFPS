using UnityEngine;
using System.Collections;

public class LevelScript : MonoBehaviour {
	void  Start (){
		var enemies= transform.FindChild("enemies");
		if(enemies){
			foreach(Transform child in enemies){
				if(Random.Range(0.0f,1.0f) < 0.9f){
					GameObject.Destroy(child.gameObject);
				}
			}
		}
		var players= transform.FindChild("player_spawn");
		if(players){
			var num = players.childCount;
			var save= Random.Range(0,num);
			var j=0;
			foreach(Transform child in players){
				if(j != save){
					GameObject.Destroy(child.gameObject);
				}
				++j;
			}
		}
		var items= transform.FindChild("items");
		if(items){
			foreach(Transform child in items){
				if(Random.Range(0.0f,1.0f) < 0.9f){
					GameObject.Destroy(child.gameObject);
				}
			}
		}
	}
}