using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Blophy.Chart;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
public class TestChart : MonoBehaviourSingleton<TestChart>
{
    public ChartData chartData;
    public AudioClip clip;

    private void Start()
    {
        AssetManager.Instance.chartData = chartData;
        AssetManager.Instance.musicPlayer.clip = clip;
    }
}
