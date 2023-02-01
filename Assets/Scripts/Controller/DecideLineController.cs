using Blophy.Chart;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.Numerics;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Blophy.Extension;
using UnityEngine.Pool;
using Quaternion = UnityEngine.Quaternion;
/*
* 声明：
* StartTime简称ST
* EndTime简称ET
* StartValue简称SV
* EndValue简称EV
* 
* 百分比解释：
* 因为Unity的AnimationCurve的Key的InWeight和OutWeight的表示
* InWeight就是这个点距离上一个点为单位，自身为0，到上一个点的距离为1的表示法
* OutWeight就是这个点距离下一个点为单位，自身为0，到下一个点的距离为1的表示法
* 这里说为百分比是觉得百分比也是0-1之间的标准数值
*/
public class DecideLineController : MonoBehaviour
{
    public float lineDistance;//线的距离，根据每帧计算，生成这个范围内的所有Note
    public bool isHorizontal;

    public AnimationCurve canvasSpeed;//这个用来表示这根线的所有速度总览
    public AnimationCurve canvasLocalOffset;//这个用来表示的是某个时间，画布的Y轴应该是多少
    public Transform onlineNote;//判定线上边的音符
    public Transform offlineNote;//判定线下边的音符

    public List<ObjectPoolQueue<NoteController>> onlineNotes;//判定线上方的音符对象池
    public List<ObjectPoolQueue<NoteController>> offlineNotes;//判定线下方的音符对象池

    public SpriteRenderer lineTexture;
    public BoxController box;
    public LineNoteController lineNoteController;//判定线音符管理脚本

    Line thisLine;//这根线的源数据
    public Line ThisLine
    {
        get => thisLine;
        set
        {
            thisLine = value;//获取到源数据
            Init();//初始化
        }
    }//这跟线的谱面元数据

    /// <summary>
    /// 初始化方法
    /// </summary>
    void Init()
    {
        InitNotesObjectPool();//初始化对象池
        SpeckleManager.Instance.allLineNoteControllers.Add(lineNoteController);
        List<Keyframe> keyframes = GameUtility.CalculatedSpeedCurve(ThisLine.speed);//将获得到的Key列表全部赋值
        canvasSpeed = new() { keys = keyframes.ToArray(), preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };//把上边获得到的点转换为速度图
        canvasLocalOffset = GameUtility.CalculatedOffsetCurve(canvasSpeed, keyframes);//吧速度图转换为位移图
        CalculatedNoteFloorPosition(ThisLine.onlineNotes);//计算判定线上方的所有音符的FloorPosition
        CalculatedNoteFloorPosition(ThisLine.offlineNotes);//计算判定线下方的所有音符的FloorPosition
    }
    /// <summary>
    /// 初始化对象池
    /// </summary>
    private void InitNotesObjectPool()
    {
        onlineNotes = new()
        {
            new ObjectPoolQueue<NoteController>(AssetManager.Instance.noteControllers[(int)NoteType.Tap],3,box.sortSeed,onlineNote),
            new ObjectPoolQueue<NoteController>(AssetManager.Instance.noteControllers[(int)NoteType.Hold],2,box.sortSeed,onlineNote),
            new ObjectPoolQueue<NoteController>(AssetManager.Instance.noteControllers[(int)NoteType.Drag],5,box.sortSeed,onlineNote),
            new ObjectPoolQueue<NoteController>(AssetManager.Instance.noteControllers[(int)NoteType.Flick],0,box.sortSeed,  onlineNote),
            new ObjectPoolQueue<NoteController>(AssetManager.Instance.noteControllers[(int)NoteType.Point],1,box.sortSeed,  onlineNote),
            new ObjectPoolQueue<NoteController>(AssetManager.Instance.noteControllers[(int)NoteType.FullFlickPink],1,box.sortSeed,  onlineNote),
            new ObjectPoolQueue<NoteController>(AssetManager.Instance.noteControllers[(int)NoteType.FullFlickBlue],1,box.sortSeed,  onlineNote)
        };
        offlineNotes = new()
        {
            new ObjectPoolQueue<NoteController>(AssetManager.Instance.noteControllers[(int)NoteType.Tap],3,box.sortSeed,offlineNote),
            new ObjectPoolQueue<NoteController>(AssetManager.Instance.noteControllers[(int)NoteType.Hold],2,box.sortSeed, offlineNote),
            new ObjectPoolQueue<NoteController>(AssetManager.Instance.noteControllers[(int)NoteType.Drag],5,box.sortSeed, offlineNote),
            new ObjectPoolQueue<NoteController>(AssetManager.Instance.noteControllers[(int)NoteType.Flick],0,box.sortSeed,  offlineNote),
            new ObjectPoolQueue<NoteController>(AssetManager.Instance.noteControllers[(int)NoteType.Point],1,box.sortSeed, offlineNote),
            new ObjectPoolQueue<NoteController>(AssetManager.Instance.noteControllers[(int)NoteType.FullFlickPink],1,box.sortSeed, offlineNote),
            new ObjectPoolQueue<NoteController>(AssetManager.Instance.noteControllers[(int)NoteType.FullFlickBlue],1,box.sortSeed, offlineNote)
        };
    }
    /// <summary>
    /// 获取音符
    /// </summary>
    /// <param name="noteType">音符类型</param>
    /// <param name="isOnlineNote">在判定线上方还是下方</param>
    /// <returns>获取到的音符</returns>
    public NoteController GetNote(NoteType noteType, bool isOnlineNote)
    {
        return isOnlineNote switch
        {
            true => onlineNotes[(int)noteType].PrepareNote(),//如果是判定线上方，就返回判定线上方的音符
            false => offlineNotes[(int)noteType].PrepareNote(),//如果是判定线下方，就返回判定线下方的音符
        };
    }
    /// <summary>
    /// 返回音符
    /// </summary>
    /// <param name="note">需要返回的音符</param>
    /// <param name="noteType">音符类型</param>
    /// <param name="isOnlineNote">判定线上方还是下方</param>
    public void ReturnNote(NoteController note, NoteType noteType, bool isOnlineNote)
    {
        switch (isOnlineNote)
        {
            case true://上方就回上方去
                onlineNotes[(int)noteType].ReturnNote(note);
                break;
            case false://下方就回下方去
                offlineNotes[(int)noteType].ReturnNote(note);
                break;
        }
    }
    /// <summary>
    /// 计算音符的FloorPosition
    /// </summary>
    /// <param name="notes"></param>
    void CalculatedNoteFloorPosition(Note[] notes)
    {
        for (int i = 0; i < notes.Length; i++)//遍历所有音符
        {
            notes[i].hitFloorPosition = (float)Math.Round(canvasLocalOffset.Evaluate(notes[i].hitTime), 3);//根据打击时间获取到打击距离
        }
    }
    private void Update()
    {
        UpdateCanvas();//更新画布
    }
    /// <summary>
    /// 更新画布
    /// </summary>
    private void UpdateCanvas()
    {
        float currentValue = canvasLocalOffset.Evaluate((float)ProgressManager.Instance.CurrentTime);//获取到当前的画布距离
        onlineNote.localPosition = Vector3.down * currentValue;//赋值
        offlineNote.localPosition = Vector3.up * currentValue;//赋值
    }
}