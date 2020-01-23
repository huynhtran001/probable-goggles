using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicPlayer : MonoBehaviour {
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip mainMenu;
    [SerializeField] AudioClip lithHarbor;
    [SerializeField] AudioClip street;
    [SerializeField] AudioClip bramble;

    [Range(0, 1)]
    [SerializeField] float setVolume = .6f;

    void Start () {
        LoadSong(SceneManager.GetActiveScene().buildIndex);
	}
	

    public void HalveVolume()
    {
        setVolume = audioSource.volume;
        audioSource.volume = setVolume / 2;
    }

    public void InitialVolume()
    {
        audioSource.volume = setVolume;
    }

    // will be called when certain areas reached or when game manager loads new scene
	public void LoadSong(int x)
    {
        switch(x)
        {
            case 1:
                MyPlay(mainMenu);
                break;
            case 2:
                MyPlay(street);
                break;
            case 5:
                MyPlay(lithHarbor);
                break;
            case 7:
                MyPlay(bramble);
                break;
            default:
                return;
        }
    }

    private void MyPlay(AudioClip clip)
    {
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }
}
