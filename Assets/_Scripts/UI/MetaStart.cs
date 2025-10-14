using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class MetaStart : MonoBehaviour
{
    [SerializeField] private float fillSpeed = 0.1f;
    [SerializeField] private Image loadingFillImage;
    [SerializeField] private TextMeshProUGUI percentageText;

    private void Start()
    {
        StartCoroutine(LoadMainGame());
    }

    private IEnumerator LoadMainGame()
    {
        float fakeProgress = 0f;
        float targetFill = 1f;

        while (fakeProgress < 1f)
        {
            fakeProgress = Mathf.MoveTowards(fakeProgress, targetFill, Time.deltaTime * fillSpeed);
            loadingFillImage.fillAmount = fakeProgress;

            int percent = Mathf.RoundToInt(fakeProgress * 100f);
            percentageText.text = percent + "%";

            yield return null;
        }

        yield return new WaitForSeconds(1.5f);


        if (LevelManager.Instance != null)
        LevelManager.Instance.GoToLevelName("MenuScene");
    }
}