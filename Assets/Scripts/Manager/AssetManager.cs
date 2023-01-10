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

    [Header("打击特效的预制件")]
    public HitEffectController hitEffect;
}
