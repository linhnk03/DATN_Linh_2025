using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] Transform contentData;
    [SerializeField] Transform contentShow;

    [SerializeField] TypeShape[] goShowData;
    [SerializeField] GameObject goItem;

    [SerializeField] Button btnStart;
    [SerializeField] Button btnQuit;
    [SerializeField] Button btnUseLocal;

    // Start is called before the first frame update
    void Start()
    {
        
        btnUseLocal.image.color = PlayerPrefs.GetInt("useLocalOptimal", 0) == 0 ? Color.green : Color.red;
        InitData();
        btnQuit.onClick.AddListener(() =>
        {
            Application.Quit();
        });

        btnStart.onClick.AddListener(() =>
        {
            List<TypeShape> shapeList = new List<TypeShape>();
            for (int i=0; i < contentShow.childCount; i++)
            {
                TypeShape t = contentShow.GetChild(i).GetComponent<ItemShowMainHome>().typeShape;
                //Debug.Log(t._name);
                shapeList.Add(t);
            }
            DataGame.SaveData(shapeList);
            //return;
            PlayerPrefs.SetInt("NumberCheck", 0);
            SceneManager.LoadScene(ValueConst.NAME_SCENE_PLAY);
        });

        btnUseLocal.onClick.AddListener(() =>
        {
            bool value = PlayerPrefs.GetInt("useLocalOptimal", 0) == 0;
            value = !value;
            PlayerPrefs.SetInt("useLocalOptimal", value ? 0 : 1);
            btnUseLocal.image.color = value ? Color.green : Color.red;
        });
    }

    void InitData()
    {
        for (int i = 0; i < goShowData.Length; i++)
        {
            GameObject go = Instantiate(goItem, contentData);
            go.GetComponent<ItemShowMainHome>().Init(goShowData[i], true, this);
        }
        btnStart.gameObject.SetActive(false);
    }

    public void Add(ItemShowMainHome item)
    {
        item.transform.SetParent(contentShow);
        item.SetUp(false);
        btnStart.gameObject.SetActive(true);
    }

    public void Remove(ItemShowMainHome item)
    {
        item.transform.SetParent(contentData);
        item.SetUp(true);
        if(contentShow.childCount ==0)
            btnStart.gameObject.SetActive(false);
    }

    public void Up(ItemShowMainHome item)
    {
        // Lấy vị trí hiện tại trong contentShow
        int index = item.transform.GetSiblingIndex();
        // Giảm index đi 1, nhưng không nhỏ hơn 0
        int newIndex = Mathf.Max(index - 1, 0);
        item.transform.SetSiblingIndex(newIndex);
    }

    public void Down(ItemShowMainHome item)
    {
        // Lấy vị trí hiện tại trong contentShow
        int index = item.transform.GetSiblingIndex();
        // Tăng index lên 1, nhưng không vượt quá (số con - 1)
        int maxIndex = contentShow.childCount - 1;
        int newIndex = Mathf.Min(index + 1, maxIndex);
        item.transform.SetSiblingIndex(newIndex);
    }
}
