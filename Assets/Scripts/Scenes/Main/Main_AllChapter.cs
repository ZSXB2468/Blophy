using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Main_AllChapter : Public_Button
{
    private void Start()
    {

        thisButton.onClick.AddListener(() => Loading_Controller.Instance.SetLoadSceneByName("SelectChapter").StartLoad());
    }
}
