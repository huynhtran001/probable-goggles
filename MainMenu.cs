using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;

public class MainMenu : MonoBehaviour {
    [Tooltip("The name of the game")]
    [SerializeField] string titleName;
    [SerializeField] MusicPlayer musicPlayer;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip menuNavigation;
    [SerializeField] AudioClip startSound;
    [SerializeField] Text title;
    [SerializeField] Button startButton;
    [SerializeField] Button quitButton;
    [SerializeField] Image panel;
    [SerializeField] Image startCursor;
    [SerializeField] Image quitCursor;
    [SerializeField] Animator anim;
    
    [Tooltip("The lower the value, the faster the text appears")]
    [SerializeField] float textSpeed = 0.02f;
    [SerializeField] float menuFunctionsLoadingTime = 2f;
    [SerializeField] float loadingTimeToNextScene = 2f;
    [SerializeField] float minimumTimeBeforeSkip = 2f;

    IEnumerator currentCoroutine;
    private bool gameStarting = false;
    private bool stillAnimating = true;
    private GameObject currentButtonFocus;

    private void Start()
    {
        audioSource.clip = menuNavigation;
        title.text = "";
        
        // sets start and quit buttons inactive so the starting animation can play. this can be skipped as seen below in the Update function
        startButton.gameObject.SetActive(false);
        quitButton.gameObject.SetActive(false);

        // starts opening animation
        currentCoroutine = TitleAnimation();
        
    }

    private void Update()
    {
        // prevents player from spamming start to hear start sound fx
        if (gameStarting) return;
        if (musicPlayer == null) musicPlayer = FindObjectOfType<MusicPlayer>();
        
        // this skips the startup animation after a minimuum specified time
        if (Input.anyKeyDown && stillAnimating && Time.timeSinceLevelLoad >= minimumTimeBeforeSkip)
        {
            StopCoroutine(currentCoroutine);
            panel.gameObject.SetActive(false);
            stillAnimating = false;
            title.text = titleName;
            EnableMenuFunctions();
        }


        // whenever a new button is highlighted, it plays the menu navigation sound
        if (EventSystem.current.currentSelectedGameObject != currentButtonFocus)
        {
            audioSource.Play();
            currentButtonFocus = EventSystem.current.currentSelectedGameObject;
            if (currentButtonFocus == startButton.gameObject)
            {
                startCursor.enabled = true;
                quitCursor.enabled = false;
            }
            else if (currentButtonFocus == quitButton.gameObject)
            {
                startCursor.enabled = false;
                quitCursor.enabled = true;
            }
        }
        
    }
    

    public void EnableMenuFunctions()
    {
        // start button set active and selected by default
        startButton.gameObject.SetActive(true);
        startButton.Select();

        // sets the "quit" button active
        quitButton.gameObject.SetActive(true);
    }

    public void ScrollingText()
    {
        if (title.text.Equals(titleName)) return;
        StartCoroutine(currentCoroutine);
    }

    IEnumerator TitleAnimation()
    {
        // triggered by animation event
        foreach (char c in titleName.ToCharArray())
        {
            title.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
        yield return new WaitForSeconds(menuFunctionsLoadingTime);
        EnableMenuFunctions();
        stillAnimating = false;
    }

    public void Begin()
    {
        if (!gameStarting)
        {
            panel.gameObject.SetActive(true);
            anim.SetTrigger("start");
            audioSource.clip = startSound;
            audioSource.Play();
            StartCoroutine(ShortLoad());
        }
    }

    IEnumerator ShortLoad()
    {
        gameStarting = true;
        yield return new WaitForSeconds(loadingTimeToNextScene);
        SceneManager.LoadScene(2);
        musicPlayer.LoadSong(2);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
