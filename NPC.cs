using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class NPC : MonoBehaviour
{
    [SerializeField] private string npcName;
    [TextArea(3,4)] [SerializeField] string[] messages;

    public PlayerMovement player;
    [SerializeField] SpriteRenderer exclamination;

    [SerializeField] float talkRadius = 4.4f;
    [SerializeField] float talkCooldown = 2f;
    private float currentElapsedTime;
    private CircleCollider2D myCollider;

    private GameManager game;
    private bool isInteractable;
    private bool currentlyTalking = false;

    private void Start()
    {
        currentElapsedTime = talkCooldown + 1;
        if (messages != null)
        {
            game = GameObject.FindObjectOfType<GameManager>();
            isInteractable = true;

        }
        exclamination.enabled = false;
        myCollider = GetComponent<CircleCollider2D>();
        myCollider.radius = talkRadius;
    }

    private void Update()
    {
        if (!isInteractable) return;
        if (currentElapsedTime >= talkCooldown)
        {
            Interact();
        }
        else
        {
            currentElapsedTime += Time.fixedDeltaTime;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, talkRadius);
    }

    public void Interact()
    {
        Vector2 currentPos = new Vector2(transform.position.x, transform.position.y);
        Collider2D possiblePlayer = Physics2D.OverlapCircle(currentPos, talkRadius, LayerMask.GetMask("Player"));
        
        if (possiblePlayer != null)
        {
            exclamination.enabled = true;
        }
        else
        {
            exclamination.enabled = false;
        }

        if (CrossPlatformInputManager.GetButtonDown("Dash") && possiblePlayer != null && !currentlyTalking)
        {
            TestForMovement(); // turn off animator and random movements
            player = possiblePlayer.GetComponent<PlayerMovement>(); // get the collider's player movement script
            currentlyTalking = true; // one tap of the spacebar will run this code many times, so run only once by turning this bool on

            player.ToggleControl();
            game.StartConversation(messages, this);
        }
    }

    public void EndConversation()
    {
        TestForMovement();
        if (player != null) player.ToggleControl();
        player = null;
        currentlyTalking = false;
        currentElapsedTime = 0f;
        exclamination.enabled = false;
        
    }

    private void TestForMovement()
    {
        AIMovement ai = GetComponent<AIMovement>();
        if (ai != null)
        {
            ai.Toggle();
        }
    }

    public string Name()
    {
        return npcName;
    }

    public bool Status()
    {
        return currentlyTalking;
    }
}
