using UnityEngine;

public class DoubleMirror : MonoBehaviour {

	private Transform playerTransform;
	private Transform playerShadowTransform;


	private void Awake() {
		playerShadowTransform = GameObject.FindGameObjectWithTag("PlayerShadow").transform;
	}

	// Start is called before the first frame update
	void Start() {
		playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
	}

	// Update is called once per frame
	void Update() {
		//Position the shadow according to the player's position
		playerShadowTransform.position = new Vector3(
			playerTransform.transform.position.x,
			playerTransform.transform.position.y,
			transform.position.z - (playerTransform.transform.position.z - transform.position.z)
		);

		//Rotate the shadow according to the player's rotation
		playerShadowTransform.rotation = Quaternion.Inverse(playerTransform.rotation);
	}
}
