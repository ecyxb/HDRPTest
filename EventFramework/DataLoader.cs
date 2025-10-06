using System.Collections.Generic;
using UnityEngine;
using EventFramework;

namespace EventFramework
{
    public class DataLoader
    {
        private static DataLoader instance;
        public static DataLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DataLoader();
                    instance.Initialize();
                }
                return instance;
            }
        }

        // 翻译字典
        private static Dictionary<string, string> translationDict = new Dictionary<string, string>();

        // 玩家基础属性映射
        public Dictionary<Const.StateConst, PlayerBaseProperty> playerBasePropertyMap = new Dictionary<Const.StateConst, PlayerBaseProperty>();

        // 状态数据映射  
        public Dictionary<int, StateData> stateDataMap = new Dictionary<int, StateData>();

        // 商店数据
        public Dictionary<int, CameraData> cameraData = new Dictionary<int, CameraData>();
        public Dictionary<int, LensData> lensData = new Dictionary<int, LensData>();
        public Dictionary<int, PlugData> plugData = new Dictionary<int, PlugData>();

        private void Initialize()
        {
            LoadPlayerBaseProperties();
            LoadStateData();
            LoadShopData();
            LoadTranslations();
        }

        private void LoadPlayerBaseProperties()
        {
            // 初始化玩家基础属性
            var baseProperty = new PlayerBaseProperty();
            baseProperty.attrMap = new Dictionary<string, UnionInt64>
        {
            { "Health", 100 },
            { "Stamina", 100 },
            { "Speed", 5 }
        };

            playerBasePropertyMap[Const.StateConst.NONE] = baseProperty;
        }

        private void LoadStateData()
        {
            // 初始化状态数据
            for (int i = 0; i < 10; i++) // 假设有10个状态
            {
                var stateData = new StateData();
                stateData.rule = new Dictionary<int, Const.StateOp>();

                // 为每个状态设置与其他状态的规则
                for (int j = 0; j < 10; j++)
                {
                    stateData.rule[j] = Const.StateOp.GET_AND_GET;
                }

                stateDataMap[i] = stateData;
            }
        }

        private void LoadShopData()
        {
            // 初始化相机数据
            cameraData[1] = new CameraData
            {
                name = "Basic Camera",
                priceText = "100",
                uiPath = "UI/Camera1",
                price = 100
            };

            // 初始化镜头数据
            lensData[1] = new LensData
            {
                name = "Basic Lens",
                priceText = "50",
                uiPath = "UI/Lens1",
                price = 50
            };

            // 初始化配件数据
            plugData[1] = new PlugData
            {
                name = "Basic Plug",
                priceText = "25",
                uiPath = "UI/Plug1",
                price = 25
            };
        }

        private void LoadTranslations()
        {
            // 初始化翻译数据
            translationDict["商店"] = "Shop";
            translationDict["镜头"] = "Lens";
            translationDict["相机"] = "Camera";
            translationDict["其他"] = "Other";
            translationDict["快门优先"] = "Shutter Priority";
            translationDict["光圈优先"] = "Aperture Priority";
            translationDict["程序自动"] = "Program Auto";
            translationDict["手动模式"] = "Manual Mode";
            translationDict["中央重点测光"] = "Center Weighted";
            translationDict["点测光"] = "Spot Metering";
            translationDict["矩阵曝光"] = "Matrix Metering";
            translationDict["平均測光"] = "Average Metering";
            translationDict["全屏对焦"] = "Full Screen Focus";
            translationDict["单点对焦"] = "Single Point Focus";
            translationDict["宽区域对焦"] = "Wide Area Focus";
        }

        // 翻译方法
        public static string Tr(string key)
        {
            if (translationDict.ContainsKey(key))
            {
                return translationDict[key];
            }
            return key; // 如果没有翻译，返回原文
        }

        public static string Tr(string key, int id)
        {
            // 带ID的翻译方法，这里简单返回翻译结果
            return Tr(key);
        }
    }
}

// 玩家基础属性类
public class PlayerBaseProperty
{
    public Dictionary<string, UnionInt64> attrMap = new Dictionary<string, UnionInt64>();
}

// 状态数据类
public class StateData
{
    public Dictionary<int, Const.StateOp> rule = new Dictionary<int, Const.StateOp>();
}