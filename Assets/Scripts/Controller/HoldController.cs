using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldController : NoteController
{
    public Transform holdBody;//音符身体
    public Transform holdHead;//音符头

    public float remainTime;//停留时间

    public bool reJudge = false;//如果已经可以重新播放特效了，有没有重新播放，默认为没有
    public bool isMissed = false;//Miss掉了
    public float reJudgeTime = 0;//距离上一次打击特效播放已经过去多久了
    public float checkTime = -.1f;//手指离开了多长时间

    NoteJudge noteJudge;
    bool isEarly;
    /// <summary>
    /// 初始化
    /// </summary>
    public override void Init()
    {
        AnimationCurve localOffset = decideLineController.canvasLocalOffset;//拿到位移图的索引
        holdBody.transform.localScale = //设置缩放
                                        //new Vector2(1, localOffset.Evaluate(thisNote.EndTime) - localOffset.Evaluate(thisNote.hitTime));//x轴默认为1，y轴为位移图上的距离
            Vector2.up * (localOffset.Evaluate(thisNote.EndTime) - localOffset.Evaluate(thisNote.hitTime)) + Vector2.right;
        isMissed = false;   //重置状态
        reJudge = false;    //重置状态
        checkTime = -.1f;   //重置状态
        reJudgeTime = 0;    //重置状态
        noteJudge = NoteJudge.Miss;
        isEarly = true;
        base.Init();
    }
    public override void Judge(double currentTime, TouchPhase touchPhase)
    {
        switch (touchPhase)//如果触摸阶段
        {
            case TouchPhase.Began://是开始阶段
                TouchPhaseBegan();
                break;//啥也不干，因为上边已经干了
            default://剩下的
                checkTime = Time.time;//更新时间
                reJudgeTime += Time.deltaTime;//累加重新判定时间
                if (reJudgeTime >= ValueManager.Instance.holdHitEffectCDTime)//如果已经到了这顶的时间，那就
                {
                    reJudgeTime = 0;//重置为0
                    reJudge = true;//设置状态为可以重新播放打击特效了
                }
                break;
        }
    }

    private void TouchPhaseBegan()
    {
        switch (isJudged)
        {
            case true:
                HitEffectManager.Instance.PlayHitEffect(transform.position, transform.rotation, ValueManager.Instance.perfectJudge);//播放打击特效
                break;
            case false://如果没有判定过并且触摸阶段是开始触摸
                isJudged = true;//设置状态为判定过了
                checkTime = Time.time;//设置时间
                JudgeLevel(out noteJudge, out isEarly);
                HitEffectManager.Instance.PlayHitEffect(transform.position, transform.rotation, GetColorWithNoteJudge(noteJudge));//播放打击特效
                break;
        }
    }

    public override void JudgeLevel(out NoteJudge noteJudge, out bool isEarly)
    {
        base.JudgeLevel(out noteJudge, out isEarly);
        noteJudge = noteJudge switch
        {
            NoteJudge.Bad => NoteJudge.Good,
            _ => noteJudge
        };
    }
    /// <summary>
    /// Hold音符Miss掉了
    /// </summary>
    public void HoldMiss()
    {
        ChangeColor(new Color(1, 1, 1, .3f));//Miss掉了就设置透明度为30%
    }
    protected override void PlayHitEffectWithJudgeLevel(NoteJudge noteJudge, Color hitJudgeEffectColor)
    {
        base.PlayHitEffectWithJudgeLevel(noteJudge, hitJudgeEffectColor);
    }
    public override void ReturnPool()
    {
        ScoreManager.Instance.AddScore(thisNote.noteType, noteJudge, isEarly);
    }
    public override void NoteHoldArise()
    {
        //这里放“现在是通过遮罩做的，我想的是，未来可不可以去掉遮罩，做成上下自动检测拥有停留时间”中的内容
        //***************************************************************************************

        if (checkTime > 0 && isJudged)//chechTime大于0说明手指有判定了，并且已经判定过了
        {
            if (Time.time - checkTime > ValueManager.Instance.holdLeaveScreenTime && !isMissed)//如果当前时间距离手指在我这里判定的最后一帧已经超过预先设置的时间了并且没有Miss
            {
                isMissed = true;//这个条件下肯定已经Miss了，设置状态
                HoldMiss();//调用Miss函数
            }
            else if (Time.time - checkTime <= ValueManager.Instance.holdHitEffectCDTime && !isMissed && reJudge)//如果当前时间距离手指在我这里判定的最后一帧没有超过预先设置的时间并且没有Miss和可以进行重判了
            {
                //checkTime = Time.time;
                //没有Miss
                //打击特效
                HitEffectManager.Instance.PlayHitEffect(transform.position, transform.rotation, ValueManager.Instance.perfectJudge);//播放打击特效
                reJudge = false;//重判一次完成后就设置状态
            }
        }
        if (ProgressManager.Instance.CurrentTime >= thisNote.hitTime + JudgeManager.bad && !isJudged && !isMissed)//如果当前时间已经超过了打击时间+bad时间（也就是一开始就没有按下手指）并且没有判定过并且没有Miss
        {
            isMissed = true;//这个条件下肯定已经Miss了，设置状态
            HoldMiss();//调用Miss函数
        }
    }

    public override void PassHitTime(double currentTime)
    {
        AnimationCurve localOffset = decideLineController.canvasLocalOffset;//拿到位移图的索引
        transform.localPosition = new Vector2(transform.localPosition.x, -noteCanvas.localPosition.y);//将位置保留到判定线的位置
        holdBody.transform.localScale = new Vector2(1, localOffset.Evaluate(thisNote.EndTime) - localOffset.Evaluate((float)currentTime));//设置缩放
    }
}
