using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class LoadingController : MonoBehaviour
{
    [SerializeField] float minLoadTime;
    [SerializeField] UnityEngine.UI.Slider slider;
    [SerializeField] TextMeshProUGUI txtShow;

    bool sceneLoaded = false;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(ShowText());
        StartCoroutine(Loading());
    }
    IEnumerator Loading()
    {
        var sceneName = ValueConst.NAME_SCENE_MENU;
        var asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        asyncOp.allowSceneActivation = false;

        float timer = 0;
        while(true)
        {
            timer += Time.deltaTime;
            float fakeProgress = Mathf.Clamp01(timer / minLoadTime);
            float realProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);
            float displayProgress = Mathf.Min(fakeProgress, realProgress);

            slider.value = Mathf.Min(fakeProgress, realProgress);

            if (asyncOp.progress >= 0.9f && fakeProgress >= 0.9f)
                break;

            yield return null;
        }
        slider.value = 1;
        yield return null;
        asyncOp.allowSceneActivation = true;
        sceneLoaded = true;
    }
    IEnumerator ShowText()
    {
        WaitForSeconds w = new WaitForSeconds(1);
        while (!sceneLoaded)
        {
            txtShow.text = "Loading";
            yield return w;
            txtShow.text = "Loading.";
            yield return w;
            txtShow.text = "Loading..";
            yield return w;
            txtShow.text = "Loading...";
            yield return w;
        }
    }
}
