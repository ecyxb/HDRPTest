using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Const
{
    public static class ColorConst
    {
        public static readonly Color highlightColorBg = new Color(1, 170 / 255f, 0, 235 / 255f);
        public static readonly Color highlightColor = new Color(1, 170 / 255f, 0, 1f);

        public static readonly Color normalColor = new Color(1, 1, 1, 1f);
    }
    public static class PhotoConst
    {
        public static readonly float TaregetBrightness = 0.35f; // 目标亮度
        public static readonly float EVToBrightness = 0.37f; // EV2对应的亮度
    }

    public enum StateConst
    {
        NONE = -1, // 这个一般不使用，仅做没有状态的占位符
        MOVE = 0,
        SPRINT = 1,
        INSPACE = 2,
        JUMP = 3,
        FOCUS = 4,
    }
    public enum StateOp : byte
    {
        GET_AND_DISCARD = 0b01, //保持当前状态，丢弃新状态
        DISCARD_AND_GET = 0b10, //丢弃当前状态，获取新状态
        GET_AND_GET = 0b00, //保持当前状态，获取新状态
        DISCARD_AND_DISCARD = 0b11, //丢弃当前状态，丢弃新状态，一般用于没有实际状态，只用作指令

        DISCARD_NEW = 0b01, //丢弃新状态
        DISCARD_OLD = 0b10, //丢弃旧状态

    }
    public enum AutoFocusArea : byte
    {
        SPOT = 1,
        MIDDLE = 2,
        FULL = 3,
    }


    public enum TargetDrawUIType : byte
    {
        CIRCLE = 0,

    }
}