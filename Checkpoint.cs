using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour {
    private GameManager manager;
    private bool checkpointReached = false;
    [Tooltip("Check this off if this is where you want the player to spawn initially")]
    [SerializeField] bool spawnpoint;


	// Use this for initialization
	void Start () {
        manager = FindObjectOfType<GameManager>();
        if (spawnpoint)
        {
            checkpointReached = true;
            Vector3 currentPos = new Vector3(transform.position.x, transform.position.y + 1.5f, transform.position.z);
            manager.CheckpointReached(currentPos);
        }
	}

    // Update is called once per frame

    private void OnTriggerEnter2D(Collider2D collision)
    {

        // if checkpoint has been reached previously, it cannot be triggered again.
        if (collision.tag == "Player" && !checkpointReached)
        {
            // added +1.5 to the Y coordinate because the player collider is 1 (~0.93ish) unit tall, so he'd clip through the floor without this
            Vector3 currentPos = new Vector3(transform.position.x, transform.position.y + 1.5f, transform.position.z);
            manager.CheckpointReached(currentPos);
            checkpointReached = true;
        }
    }

}
