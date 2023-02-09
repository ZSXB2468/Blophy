using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullFlickController : NoteController
{
    public Transform textureBoss;//就是FullFlick音符的两个渲染贴图（Texture）的爸爸（
    public bool isMoved = false;//是否已经移动了
    int decisionEndPoint;
    public override void Init()
    {
        base.Init();
        isMoved = false;//重置isMoved
        switch (thisNote.isClockwise)//顺时针逆时针
        {
            case true://如果是顺时针
                decisionEndPoint = -1;//终点在x=-1位置
                textureBoss.localRotation = Quaternion.Euler(Vector3.forward * 180);//旋转180度
                break;
            case false://如果是逆时针
                decisionEndPoint = 1;//终点在x=1位置
                textureBoss.localRotation = Quaternion.identity;//默认旋转即可
                break;
        }
    }
    public override void Judge(double currentTime, TouchPhase touchPhase)
    {
        if (!isJudged && touchPhase == TouchPhase.Began)//如果没有判定过并且是开始阶段
        {
            isJudged = true;//是否判定过为tue
        }
        else if (isJudged && touchPhase == TouchPhase.Moved)//如果判定过并且判定阶段为moved
        {
            isMoved = true;//设置为真
        }
    }
    public override void Judge()
    {
        switch (isJudged)
        {
            case true:
                Judge(thisNote.hitTime, TouchPhase.Moved);
                break;
            case false:
                Judge(thisNote.hitTime, TouchPhase.Began);
                break;
        }
    }
    public override void PassHitTime(double currentTime)
    {
        base.PassHitTime(currentTime);//执行基类的方法
        //这里放CurrentX，X的数据是-1-1之间的数据，理论上应该根据时间，计算出当前X
        float currentX = transform.localPosition.x;//默认赋值当前的LocalPosition.X
        if (isJudged && isMoved)//如果判定成功
        //if (true)
        {
            float percent = ((float)currentTime - thisNote.hitTime) / thisNote.HoldTime;//计算当前时间距离开始和结束过去了百分之多少
            currentX = (decisionEndPoint - thisNote.positionX) * percent + thisNote.positionX;//赋值计算得到的值，1是方框最右边，因为方框最左边是-1，左右边是1，中间是0
        }
        transform.localPosition = new Vector2(currentX, -noteCanvas.localPosition.y);//维持位置到“x和-y（本地坐标轴）”
    }
    public override void ReturnPool()
    {
        if (isJudged && isMoved)//如果判定成功
        {
            PlayEffect(NoteJudge.Perfect, ValueManager.Instance.perfectJudge, true);
            return;//返回
        }
        ScoreManager.Instance.AddScore(thisNote.noteType, NoteJudge.Miss, true);//按Miss判定加分
    }
    public override bool IsinRange(Vector2 currentPosition)
    {
        float inThisLine = noteCanvas.InverseTransformPoint(currentPosition).x;//将手指的世界坐标转换为局部坐标后的x拿到

        if (inThisLine <= ValueManager.Instance.fullFlick_noteRightJudgeRange &&//如果x介于ValueManager设定的数值之间
            inThisLine >= ValueManager.Instance.fullFlick_noteLeftJudgeRange)
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
