using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform : MonoBehaviour {
    [SerializeField] float cycleLength = 5f;

    [SerializeField] Vector2 movementDirection;
    [SerializeField] Rigidbody2D rb;

    Vector2 startingPos;

	// Use this for initialization
	void Start () {
        startingPos = new Vector2(transform.position.x, transform.position.y);
	}
	
	// Update is called once per frame
	void Update () {
        float k = Oscillate(); //value between 0 and 1 using sin function to have a smooth oscillation
        Vector2 offset = movementDirection * k; //multiplies the vector by a scale
        transform.position = startingPos + offset; // the new vector is added to initial position and loops back repeatedly.
	}

    float Oscillate()
    {
        float interval = Time.timeSinceLevelLoad / cycleLength;
        float rawCosWave = -1 * Mathf.Cos(Mathf.PI * interval);
        return rawCosWave / 2f + 0.5f;
    }
}
