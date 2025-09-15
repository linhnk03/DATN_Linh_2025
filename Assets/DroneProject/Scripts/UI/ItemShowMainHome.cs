using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemShowMainHome : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI txtName;
    [SerializeField] Button btnAdd;
    [SerializeField] Button btnUp;
    [SerializeField] Button btnDown;
    [SerializeField] Button btnDelete;
    [SerializeField] Image imgShow;
    [SerializeField] Transform transBtn;

    public TypeShape typeShape;
    MenuManager menuManager;
    public void Init(TypeShape typeShape, bool isMenuSelect, MenuManager menuManager)
    {
        this.typeShape = typeShape;
        gameObject.name = typeShape._name;
        SetUp(isMenuSelect);
        txtName.text = typeShape._der;
        imgShow.sprite = typeShape._sprite;
        this.menuManager = menuManager;

        btnAdd.onClick.AddListener(() =>
        {
            menuManager.Add(this);
        });
        btnUp.onClick.AddListener(() => {
            menuManager.Up(this);
        });
        btnDown.onClick.AddListener(() => {
            menuManager.Down(this);
        });
        btnDelete.onClick.AddListener(() =>
        {
            menuManager.Remove(this);
        });
    }
    public void SetUp(bool isMenuSelect)
    {
        transBtn.GetChild(0).gameObject.SetActive(false);
        transBtn.GetChild(1).gameObject.SetActive(false);
        transBtn.GetChild(isMenuSelect ? 0 : 1).gameObject.SetActive(true);
    }
}
