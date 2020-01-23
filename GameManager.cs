using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;

public class GameManager : MonoBehaviour {
    private Queue<string> dialogue = new Queue<string>();
    private NPC npc;
    //player controls
    [SerializeField] GameObject playerControls;

    // NPC UI
    [SerializeField] GameObject npcUI;
    [SerializeField] Text chat;
    [SerializeField] Text npcName;
    [SerializeField] Text continueToNext;
    [Tooltip("This is just for smoothness to transition into NPC conversations")]
    [SerializeField] float fadeInLength = .1f;
    [SerializeField] float textSpeed = .02f;
    public bool firstConversation = true;
    public bool endingConversation = false;

    // NPC chat / interactables
    [SerializeField] MusicPlayer musicPlayer;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip startConversation;
    [SerializeField] AudioClip nextConversation;
    [SerializeField] AudioClip endConversation;
    [SerializeField] AudioClip menuNavigation;
    private IEnumerator currentCoroutine;
    private string currentString;

    // pause menu
    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject settingsMenu;
    [SerializeField] Button resumeButton;
    [SerializeField] Button respawnButton;
    [SerializeField] Button optionsButton;
    [SerializeField] Button quitButton;
    [SerializeField] Image resumeCursor;
    [SerializeField] Image respawnCursor;
    [SerializeField] Image optionsCursor;
    [SerializeField] Image quitCursor;
    [SerializeField] AudioClip pauseSFX;
    [SerializeField] Slider musicVolume;
    [SerializeField] Image musicVolumeCursor;
    [SerializeField] Slider sfxVolume;
    [SerializeField] Image sfxVolumeCursor;
    [SerializeField] Button backSettingsButton;
    [SerializeField] Image backSettingsCursor;
    private GameObject currentButtonFocus;
    private bool isPaused = false;
    private PlayerMovement player;
    private bool inSettings = false;

    // checkpoint
    private Vector3 lastCheckpoint;

    // winning or goal reached
    [SerializeField] Animator anim;
    [SerializeField] float winDuration = 2.5f;
    [SerializeField] AudioClip winSFX;
    [Range(0, 1)]
    [SerializeField] float winVolume = 0.8f;


    // Use this for initialization
    private void Awake()
    {
        int x = FindObjectsOfType<GameManager>().Length;

        if (x > 1)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Update()
    {
        if ((CrossPlatformInputManager.GetButtonDown("Jump") || CrossPlatformInputManager.GetButtonDown("Dash")) && npc != null && !endingConversation)
        // put in here because without the ending conversation, player can spam jump to repeat coroutine. mainly just sound bug
        {
            ContinueConversation();
        }
        PauseMenuFunctions();
        if (player == null)
        {
            SingletonFix();
        }
    }

    private void SingletonFix()
    {
        /* Because the game manager doesn't destory itself on load, the player in the new map is already attached to the previous game manager that was
         * destroyed due to the singleton pattern, which breaks prefabs and references set to each other. Because of this, we must set out
         * pause menu functionality correctly via this method. Game manager audio is not included here because the slider is a childed object, 
         * and will always be correctly referenced through level loads.
         */
        player = FindObjectOfType<PlayerMovement>();
        if (player == null) return;
        respawnButton.onClick.AddListener(() => player.Respawn());
        AudioSource playerAudio = player.GetComponent<AudioSource>();
        playerAudio.volume = sfxVolume.value;
        sfxVolume.onValueChanged.AddListener((float f) => player.SetVolume(f));
    }

    private void PauseMenuFunctions()
    {
        if (!isPaused) return;
        if (EventSystem.current.currentSelectedGameObject != currentButtonFocus)
        {
            menuNavigate();
            currentButtonFocus = EventSystem.current.currentSelectedGameObject;

            if (currentButtonFocus == resumeButton.gameObject)
            {
                resumeCursor.enabled = true;
                respawnCursor.enabled = false;
                optionsCursor.enabled = false;
                quitCursor.enabled = false;
            }
            else if (currentButtonFocus == respawnButton.gameObject)
            {
                resumeCursor.enabled = false;
                respawnCursor.enabled = true;
                optionsCursor.enabled = false;
                quitCursor.enabled = false;
            }
            else if (currentButtonFocus == optionsButton.gameObject)
            {
                resumeCursor.enabled = false;
                respawnCursor.enabled = false;
                optionsCursor.enabled = true;
                quitCursor.enabled = false;
            }
            else if (currentButtonFocus == quitButton.gameObject)
            {
                resumeCursor.enabled = false;
                respawnCursor.enabled = false;
                optionsCursor.enabled = false;
                quitCursor.enabled = true;
            }

            // settings portion
            if (inSettings)
            { 
                if (currentButtonFocus == musicVolume.gameObject)
                {
                    musicVolumeCursor.enabled = true;
                    sfxVolumeCursor.enabled = false;
                    backSettingsCursor.enabled = false;
                }
                else if (currentButtonFocus == sfxVolume.gameObject)
                {
                    musicVolumeCursor.enabled = false;
                    sfxVolumeCursor.enabled = true;
                    backSettingsCursor.enabled = false;
                }
                else if (currentButtonFocus == backSettingsButton.gameObject)
                {

                    musicVolumeCursor.enabled = false;
                    sfxVolumeCursor.enabled = false;
                    backSettingsCursor.enabled = true;
                }
            }
            // ---- end settings
        }
    }

    // calls from the player to receive transform and position to respawn
    public void PlayerPause(PlayerMovement player)
    {
        if (inSettings)
        {
            CheckToLeaveSettings();
            return;
        }

        this.player = player;
        ActivatePauseMenu();
    }

    public void ActivatePauseMenu()
    {
        if (npc != null || inSettings) return;

        if (!isPaused)
        {
            Time.timeScale = 0; // freezes time
            pauseMenu.SetActive(true); // activates pause menu UI
            resumeButton.Select(); // 
            if (player != null) player.ToggleControl(); 
            resumeCursor.enabled = true;
            audioSource.PlayOneShot(pauseSFX);
            currentButtonFocus = resumeButton.gameObject;
            isPaused = true;
        }
        else
        {
            Time.timeScale = 1;
            respawnCursor.enabled = false;
            optionsCursor.enabled = false;
            quitCursor.enabled = false;
            StartCoroutine(TogglePlayerControl()); // so that when the player presses jump, it doesn't jump when it unpauses
            audioSource.PlayOneShot(pauseSFX);
            pauseMenu.SetActive(false);
            isPaused = false;
        }
    }

    IEnumerator TogglePlayerControl()
    {
        yield return new WaitForSeconds(.1f);
        if (player != null) player.ToggleControl();
    }

    public void ShowSettings()
    {
        resumeButton.gameObject.SetActive(false);
        respawnButton.gameObject.SetActive(false);
        optionsButton.gameObject.SetActive(false);
        quitButton.gameObject.SetActive(false);
        settingsMenu.SetActive(true);
        backSettingsButton.Select();
        backSettingsCursor.enabled = true;
        musicVolumeCursor.enabled = false;
        currentButtonFocus = backSettingsButton.gameObject;
        inSettings = true;
    }

    public void CheckToLeaveSettings()
    {
        resumeButton.gameObject.SetActive(true);
        respawnButton.gameObject.SetActive(true);
        optionsButton.gameObject.SetActive(true);
        quitButton.gameObject.SetActive(true);
        settingsMenu.SetActive(false);
        currentButtonFocus = optionsButton.gameObject;
        optionsButton.Select();
        inSettings = false;
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);
        musicPlayer.LoadSong(1);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void GetItem(string item)
    {
        Debug.Log("Got " + item);
    }

    public void CheckpointReached(Vector3 checkpointPos)
    {
        lastCheckpoint = checkpointPos;
    } 

    // should only be called by the player when the player object is destroyed
    public void Respawn(GameObject player)
    {
        player.transform.position = lastCheckpoint;
    }

    public void Win()
    {
        audioSource.PlayOneShot(winSFX, winVolume);
        anim.SetTrigger("win");
        StartCoroutine(WinSequence());
    }

    IEnumerator WinSequence()
    {
        yield return new WaitForSeconds(winDuration);
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentScene + 1);
        musicPlayer.LoadSong(currentScene + 1);
    }

    // call this whenever you want to hear the clip that is assigned to menuNavigation
    public void menuNavigate()
    {
        audioSource.clip = menuNavigation;
        audioSource.Play();
    }

    public void StartConversation(string[] msgs, NPC a)
    {
        dialogue.Clear();
        npc = a;
        foreach (string s in msgs)
        {
            dialogue.Enqueue(s);
        }

        musicPlayer.HalveVolume();
        Time.timeScale = 0;
        npcUI.SetActive(true);
        chat.enabled = true;
        npcName.enabled = true;
        npcName.text = npc.Name();


        if (firstConversation)
        {
            audioSource.clip = startConversation;
            audioSource.Play();
            firstConversation = false;
            continueToNext.text = "Continue";
            currentCoroutine = ScrollingText(dialogue.Dequeue());
            StartCoroutine(currentCoroutine);
            return;
        }

    }

    public void ContinueConversation()
    {
        continueToNext.gameObject.SetActive(false);

        if (dialogue.Count > 0)
        {
            continueToNext.text = "Continue";
        }
        else
        {
            continueToNext.text = "End";
        }

        if(!chat.text.Equals(currentString) && currentCoroutine != null) // if the current chat display doesnt equal to the current string, stop the coroutine, then set chat to current message
                                             // and return. this is so if the player clicks next when its still "typing," it skips the typing effect
        {
            StopCoroutine(currentCoroutine);
            chat.text = currentString;
            continueToNext.gameObject.SetActive(true);
            return;
        }


        if (dialogue.Count <= 0 ) 
        {
            StartCoroutine("FadeIn");
            return;
        }
        audioSource.clip = nextConversation;
        audioSource.Play();
        currentCoroutine = ScrollingText(dialogue.Dequeue());
        StartCoroutine(currentCoroutine);
    }

    IEnumerator ScrollingText(string msg)
    {
        chat.text = "";
        currentString = msg;
        foreach (char c in msg.ToCharArray())
        {
            chat.text += c;
            yield return new WaitForSecondsRealtime(textSpeed);
        }

        continueToNext.gameObject.SetActive(true);
    }

    IEnumerator FadeIn()
    {
        Animator anim = npcUI.GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("fade");
        audioSource.clip = endConversation;
        audioSource.Play();
        endingConversation = true;
        yield return new WaitForSecondsRealtime(fadeInLength);
        firstConversation = true;
        endingConversation = false;
        if (npc != null) npc.EndConversation();
        npc = null;
        Time.timeScale = 1;
        musicPlayer.InitialVolume();
        npcUI.SetActive(false);
    }
}
