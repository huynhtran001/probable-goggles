using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformLauncher : MonoBehaviour {
    [SerializeField] Rigidbody2D rb;
    [SerializeField] float speed = 25f;
    [SerializeField] float respawnTimer = 5f;
    [Tooltip("Additional time before destorying the object. Set to 0 to destroy at the same time when the object respawns")]
    [SerializeField] float destroyTimer = 0f;

    [Tooltip("The box will move left instead of right")]
    [SerializeField] bool left;
    private int direction = 1;
    private Vector3 initialPos;
    private bool offCooldown = true;

    private void Start()
    {
        initialPos = transform.position;
        if (left) direction = -1;
    }

    private void Update()
    {
        if (Mathf.Abs(rb.velocity.x) > 1 && offCooldown)
        {
            StartCoroutine(SpawnNewCrate());
            offCooldown = false;
        }
    }

    public void StartMoving()
    {
        rb.velocity = new Vector2(direction * speed, rb.velocity.y);
    }

    IEnumerator SpawnNewCrate()
    {
        yield return new WaitForSeconds(respawnTimer);
        Instantiate(gameObject, initialPos, Quaternion.identity);
        Destroy(gameObject, destroyTimer);
    }
}
