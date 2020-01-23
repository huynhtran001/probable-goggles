using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMovement : MonoBehaviour {

    [SerializeField] float moveSpeed = 200f;
    [SerializeField] float interval = 4f;
    [SerializeField] float elapsedTime = 0f;
    [SerializeField] float moveDuration;
    [SerializeField] float lo;
    [SerializeField] float hi;
    [SerializeField] int direction;
    [SerializeField] Vector2 ab;

    [SerializeField] Rigidbody2D rb;
    [SerializeField] Animator anim;
    [SerializeField] SpriteRenderer sprite;

    public bool standStill;

	// Use this for initialization
	void Start ()
    { 
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
    }
	
	// Update is called once per frame
	void FixedUpdate ()
    {
        if (standStill) return; 
        elapsedTime += Time.deltaTime;

        if (elapsedTime > interval)
        {
            StartCoroutine(Move());
            elapsedTime = 0f;
            interval = Random.Range(3, 5);
        }
	}

    IEnumerator Move()
    {
        direction = Random.Range(0, 2);
        moveDuration = Random.Range(lo, hi);
        if (direction == 1)
        {
            sprite.flipX = false;
            rb.velocity = new Vector2(moveSpeed * Time.deltaTime, rb.velocity.y);
            anim.SetFloat("speed", Mathf.Abs(rb.velocity.x));
            yield return new WaitForSeconds(moveDuration);
            rb.velocity = new Vector2(0, rb.velocity.y);
            anim.SetFloat("speed", Mathf.Abs(rb.velocity.x));
        }
        else
        {
            sprite.flipX = true;
            rb.velocity = new Vector2(-1 * moveSpeed * Time.deltaTime, rb.velocity.y);
            anim.SetFloat("speed", Mathf.Abs(rb.velocity.x));
            yield return new WaitForSeconds(moveDuration);
            rb.velocity = new Vector2(0, rb.velocity.y);
            anim.SetFloat("speed", Mathf.Abs(rb.velocity.x));
        }
        elapsedTime -= moveDuration;
    }

    public void Toggle()
    {
        standStill = !standStill;
    }
}
