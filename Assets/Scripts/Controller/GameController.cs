using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviourSingleton<GameController>
{
    private IEnumerator Start()
    {
        for (int i = 0; i < AssetManager.Instance.chartData.boxes.Length; i++)
        {
            Instantiate(AssetManager.Instance.boxController, AssetManager.Instance.box)
                .SetSortSeed(i * ValueManager.Instance.noteRendererOrder)//这里的3是每一层分为三小层，第一层是方框渲染层，第二和三层是音符渲染层，有些音符占用两个渲染层，例如Hold，FullFlick
                .Init(AssetManager.Instance.chartData.boxes[i]);
        }
        yield return new WaitForSeconds(3);//等8秒
        StateManager.Instance.IsStart = true;//设置状态IsStart为True
        yield return new WaitForSeconds(AssetManager.Instance.chartData.globalData.musicLength + 3f + GlobalData.Instance.chartData.globalData.offset + GlobalData.Instance.offset);
        Loading_Controller.Instance.SetLoadSceneByName("End").StartLoad();
    }
}
