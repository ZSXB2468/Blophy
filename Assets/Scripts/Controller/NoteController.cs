using Blophy.Chart;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NoteController : MonoBehaviour
{
    public List<RenderOrder> renderOrder;//渲染层级
    public int length_renderOrder = -1;//一共多少层
    public int Length_renderOrder
    {
        get
        {
            if (length_renderOrder < 0)//如果小于0说明没有调用过
                length_renderOrder = renderOrder.Count;//赋值
            return length_renderOrder;//返回
        }
    }
    public Note thisNote;//负责储存这个音符的一些数据
    public DecideLineController decideLineController;//判定线引用
    public SpriteRenderer texture;
    public Sprite commonTexture;
    public Sprite multiTexture;

    public bool isOnlineNote;//是否事判定线上方的音符
    public bool isJudged = false;//是否已经判定过了

    public Transform noteCanvas;//音符画布的引用

    public float PointNoteCurrentOffset => decideLineController.canvasLocalOffset.Evaluate((float)ProgressManager.Instance.CurrentTime);//根据当前速度计算出按照当前的位移就是现在的Point应该在的位置
    /// <summary>
    /// 从对象池出来召唤一次
    /// </summary>
    public virtual void Init()
    {
        MultiOrCommon();
        ChangeColor(Color.white);//初始化为白色
        isJudged = false;//重置isJudged

    }//初始化方法
    public virtual void MultiOrCommon() =>
        texture.sprite = thisNote.hasOther switch
        {
            true => multiTexture,
            false => commonTexture
        };
    /// <summary>
    /// 更改颜色
    /// </summary>
    /// <param name="targetColor"></param>
    protected void ChangeColor(Color targetColor)
    {
        for (int i = 0; i < Length_renderOrder; i++)//循环渲染层级的每一层
        {
            for (int j = 0; j < renderOrder[i].Length_spriteRenderers; j++)//循环每一层的所有素材
            {
                renderOrder[i].tierCount[j].color = targetColor;//改为目标颜色
            }
        }
    }

    /// <summary>
    /// 这里是被子类重写的方法，用于执行某些音符判定后特性的方法(非空)
    /// </summary>
    public virtual void Judge(double currentTime, TouchPhase touchPhase)
    {
        ReturnObjectPool();//返回对象池
    }
    /// <summary>
    /// 这里是被子类重写的方法，用于Autoplay判定
    /// </summary>
    public virtual void Judge()
    {
        ReturnObjectPool();//返回对象池
    }
    /// <summary>
    /// 吧音符返回对象池
    /// </summary>
    protected void ReturnObjectPool()
    {
        switch (isOnlineNote)//看看自己试线上的音符还是线下的音符
        {
            case true://线上的音符的话就从两个线上排序中移除自己
                decideLineController.lineNoteController.ariseOnlineNotes.Remove(this);//hitTime排序中移除自己
                decideLineController.lineNoteController.endTime_ariseOnlineNotes.Remove(this);//endTime排序中移除自己
                break;
            case false://线下的音符的话就从两个线下排序中移除自己
                decideLineController.lineNoteController.ariseOfflineNotes.Remove(this);//hitTime排序中移除自己
                decideLineController.lineNoteController.endTime_ariseOfflineNotes.Remove(this);//endTime排序中移除自己
                break;
        }
        ReturnPool();//调用返回对象池才会调用的方法
        decideLineController.ReturnNote(this, thisNote.noteType, isOnlineNote);//把自己返回对象池
    }

    /// <summary>
    /// 如果当前音符超过了打击时间并且没有销毁的这段时间，每帧调用（非空）
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    public virtual void PassHitTime(double currentTime)
    {
        //UIManager.Instance.DebugTextString = $"我是{thisNote.noteType},我应该在第{thisNote.hitTime}被打击，我是PassHitTime触发的";
        float currentAlpha = (float)(currentTime - thisNote.hitTime) / thisNote.HoldTime;//当前时间-打击时间/持续时间  可以拿到当前时间相对于打击时间到Miss这段时间的百分比
        for (int i = 0; i < Length_renderOrder; i++)//遍历每一层渲染层
        {
            for (int j = 0; j < renderOrder[i].Length_spriteRenderers; j++)//遍历每一层中需要动手脚的素材
            {
                Color changeBeforeColor = renderOrder[i].tierCount[j].color;//记录一下修改前的Color值
                renderOrder[i].tierCount[j].color =//rgb保持不变，当前alpha=1-currentAlpha
                    new Color(changeBeforeColor.r, changeBeforeColor.g, changeBeforeColor.b, 1 - currentAlpha);
            }
        }
    }
    /// <summary>
    /// 音符出现的时候每帧调用（空）
    /// </summary>
    public virtual void NoteHoldArise() { }
    /// <summary>
    /// 返回对象池调用一次（非空）
    /// </summary>
    public virtual void ReturnPool()
    {
        JudgeLevel(out NoteJudge noteJudge, out bool isEarly);//获得到判定等级，Perfect？Good？Bad？Miss？Early？Late？
        Color hitJudgeEffectColor = GetColorWithNoteJudge(noteJudge);//根据判定等级获得到打击特效的颜色

        PlayEffect(noteJudge, hitJudgeEffectColor, isEarly);
    }
    public virtual void PlayRipple() => decideLineController.box.PlayRipple();
    public virtual void PlayEffect(NoteJudge noteJudge, Color hitJudgeEffectColor, bool isEarly)
    {
        PlayEffectWithoutAddScore(noteJudge, hitJudgeEffectColor, isEarly);
        ScoreManager.Instance.AddScore(thisNote.noteType, noteJudge, isEarly);//加分
    }
    public virtual void PlayEffectWithoutAddScore(NoteJudge noteJudge, Color hitJudgeEffectColor, bool isEarly)
    {
        switch (noteJudge)//判定等级枚举
        {
            case NoteJudge.Miss://如果是Miss则不播放打击特效
                break;
            default:
                PlayEffectWithoutMiss(hitJudgeEffectColor);
                break;
        }
    }

    private void PlayEffectWithoutMiss(Color hitJudgeEffectColor)
    {
        if (thisNote.effect.HasFlag(NoteEffect.Ripple))
        {
            PlayRipple();
        }
        if (thisNote.effect.HasFlag(NoteEffect.CommonEffect))
        {
            PlayHitEffectWithJudgeLevel(hitJudgeEffectColor);//根据判定等级播放打击特效
        }

    }

    /// <summary>
    /// 根据判定等级播放打击特效
    /// </summary>
    /// <param name="noteJudge">判定等级</param>
    /// <param name="hitJudgeEffectColor">判定等级对应的颜色</param>
    protected virtual void PlayHitEffectWithJudgeLevel(Color hitJudgeEffectColor)
    {
        Vector2 notePosition = new(transform.position.x, decideLineController.lineTexture.transform.position.y);//水平判定线拿到自己的x和线渲染器的y（都是世界坐标）作为打击特效的生成点
        if (!decideLineController.isHorizontal)
        {
            notePosition = new(decideLineController.lineTexture.transform.position.x, transform.position.y);//水平判定线拿到线渲染器的x和自己的y（都是世界坐标）作为打击特效的生成点
        }
        HitEffectManager.Instance.PlayHitEffect(notePosition, transform.rotation, hitJudgeEffectColor);//播放打击特效


    }
    /// <summary>
    /// 根据音符判定等级获得到颜色
    /// </summary>
    /// <param name="noteJudge"></param>
    /// <returns></returns>
    protected static Color GetColorWithNoteJudge(NoteJudge noteJudge)
    {
        return noteJudge switch
        {
            NoteJudge.Perfect => ValueManager.Instance.perfectJudge,//如果是P、G、B就拿到对应的颜色
            NoteJudge.Good => ValueManager.Instance.goodJudge,      //如果是P、G、B就拿到对应的颜色
            NoteJudge.Bad => ValueManager.Instance.badJudge,        //如果是P、G、B就拿到对应的颜色
            _ => ValueManager.Instance.otherJudge,//其他则Other，这个颜色的出现代表有bug
        };
    }
    /// <summary>
    /// 判定等级
    /// </summary>
    /// <param name="noteJudge">输出判定等级</param>
    /// <param name="isEarly">输出早还是晚</param>
    public virtual void JudgeLevel(out NoteJudge noteJudge, out bool isEarly)
    {
        float currentTime = (float)ProgressManager.Instance.CurrentTime;//获取到当前时间
        noteJudge = NoteJudge.Miss;//默认Miss
        isEarly = true;//默认是早的
        if (currentTime <= thisNote.hitTime + JudgeManager.perfect &&
            currentTime >= thisNote.hitTime - JudgeManager.perfect)//如果在hitTime+perfect和hitTime-perfect之间
        {
            noteJudge = NoteJudge.Perfect;//完美判定
        }
        else if (currentTime <= thisNote.hitTime &&//如果在打击时间-good到打击时间之间
            currentTime >= thisNote.hitTime - JudgeManager.good)
        {
            noteJudge = NoteJudge.Good;//Good判定，Early默认是True，所以这里不理会isEarly
        }
        else if (currentTime <= thisNote.hitTime + JudgeManager.good &&
            currentTime >= thisNote.hitTime)//如果在打击时间+good到打击时间之间
        {
            noteJudge = NoteJudge.Good;//Good判定
            isEarly = false;//LateGood设置
        }
        else if (currentTime <= thisNote.hitTime &&//如果在打击时间-bad到打击时间之间
            currentTime >= thisNote.hitTime - JudgeManager.bad)
        {
            noteJudge = NoteJudge.Bad;//Bad判定
        }
        else if (currentTime <= thisNote.hitTime + JudgeManager.bad &&
            currentTime >= thisNote.hitTime)//如果在打击时间+bad到打击时间之间
        {
            noteJudge = NoteJudge.Bad;//Bad判定
            isEarly = false;//LateBad设置
        }
    }
    /// <summary>
    /// 判定触摸是否在音符的数轴判定范围内（非空）
    /// </summary>
    /// <param name="currentPosition">当前位置</param>
    /// <returns>是否在判定范围内</returns>
    public virtual bool IsinRange(Vector2 currentPosition)
    {
        //float onlineJudge = ValueManager.Instance.onlineJudgeRange;
        //float offlineJudge = ValueManager.Instance.offlineJudgeRange;

        float inThisLine = transform.InverseTransformPoint(currentPosition).x;//将手指的世界坐标转换为局部坐标后的x拿到

        if (inThisLine <= ValueManager.Instance.noteRightJudgeRange &&//如果x介于ValueManager设定的数值之间
            inThisLine >= ValueManager.Instance.noteLeftJudgeRange)
        {
            //UIManager.Instance.DebugTextString = $"onlineJudge:{onlineJudge}||offlineJudge:{offlineJudge}||Result:true ||inThisLine:{inThisLine}";
            return true;//返回是
        }
        else
        {
            //UIManager.Instance.DebugTextString = $"onlineJudge:{onlineJudge}||offlineJudge:{offlineJudge}||Result:false||inThisLine:{inThisLine}";
            return false;//返回否
        }
    }

}
[Serializable]
public class RenderOrder//每一层渲染层
{
    public List<SpriteRenderer> tierCount;//每一层渲染层下的所有需要修改的渲染组件
    public int length_tierCount = -1;//长度
    public int Length_spriteRenderers
    {
        get
        {
            if (length_tierCount < 0)//如果小于0说明没有调用过
                length_tierCount = tierCount.Count;//调用一次
            return length_tierCount;//返回
        }
    }
}