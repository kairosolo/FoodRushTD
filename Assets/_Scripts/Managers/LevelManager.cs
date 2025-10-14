using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
 

public class LevelManager : MonoBehaviour
{

    public static LevelManager Instance { get; private set; }

    [SerializeField] private GameObject loadingScreen;


    [Header("Debug Only")]
    public bool isLoading = false;
    public bool isLevelInitialized = false;


    private Animator _loadingScreenAnim;

 
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (loadingScreen != null)
        {
            _loadingScreenAnim = loadingScreen.GetComponent<Animator>();
        }

    }


    public void GoToLevelID(int levelID)
    {
        isLevelInitialized = false;
        if (!isLoading)
        {
            StartCoroutine(LoadLevelInt(levelID, 2f));
        }
    }

    public void GoToLevelName(string levelName)
    {
        isLevelInitialized = false;
        if (!isLoading)
        {
            StartCoroutine(LoadLevelString(levelName, 2f));
        }
    }

    public void NextLevel()
    {
        isLevelInitialized = false;
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (!isLoading)
        {
            StartCoroutine(LoadLevelInt(currentSceneIndex + 1, 2f));
        }
    }

    public void RestartLevel()
    {
        if (!isLoading)
        {
            isLoading = true;
            StartCoroutine(LoadLevelInt(SceneManager.GetActiveScene().buildIndex, 0f));
        }
    }


    private IEnumerator LoadLevelString(string levelName, float levelLoadDelay)
    {
        isLoading = true;

        if (_loadingScreenAnim != null)
        {
            _loadingScreenAnim.SetTrigger("Start");
            yield return new WaitForSeconds(_loadingScreenAnim.GetCurrentAnimatorStateInfo(0).length);
        }
        yield return new WaitForSeconds(levelLoadDelay);
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(levelName);
        while (!loadOperation.isDone)
        {
            yield return null;
        }

        if (_loadingScreenAnim != null)
        {
            _loadingScreenAnim.SetTrigger("End");
            yield return new WaitForSeconds(_loadingScreenAnim.GetCurrentAnimatorStateInfo(0).length);
        }

        isLoading = false;
    }

    private IEnumerator LoadLevelInt(int levelIndex, float levelLoadDelay)
    {
        isLoading = true;

        if (_loadingScreenAnim != null)
        {
            _loadingScreenAnim.SetTrigger("Start");
            yield return new WaitForSeconds(_loadingScreenAnim.GetCurrentAnimatorStateInfo(0).length);
        }
        yield return new WaitForSeconds(levelLoadDelay);
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(levelIndex);
        while (!loadOperation.isDone)
        {
            yield return null;
        }


        if (_loadingScreenAnim != null)
        {
            _loadingScreenAnim.SetTrigger("End");
            yield return new WaitForSeconds(_loadingScreenAnim.GetCurrentAnimatorStateInfo(0).length);
        }

    }

}