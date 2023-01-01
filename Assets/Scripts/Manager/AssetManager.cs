using UnityEngine;
using Blophy.Chart;
using System.Collections.Generic;

public class AssetManager : MonoBehaviourSingleton<AssetManager>
{
    [Header("铺面数据")]
    public ChartData chartData;

    [Header("音乐播放")]
    public AudioSource musicPlayer;

    [Header("方框以及他们的爹爹~")]
    public Transform box;
    public BoxController boxController;

    [Header("音符萌~")]
    public NoteController[] noteControllers;

    [Header("程序自动获取到的数值萌~")]
    public int currentTargetFPS;
    protected override void OnAwake()
    {
        currentTargetFPS = Application.isEditor switch
        {
            true => ValueManager.Instance.editorTargetFPS,
            false => Screen.currentResolution.refreshRate
        };
    }

}
