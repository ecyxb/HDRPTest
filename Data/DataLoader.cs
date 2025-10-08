using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Google.Protobuf;
using CData;

public sealed class DataLoader
{
    public Dictionary<int, CameraData> cameraData { get; private set; } = new Dictionary<int, CameraData>();
    public Dictionary<int, LensData> lensData { get; private set; } = new Dictionary<int, LensData>();
    public Dictionary<int, PlugData> plugData { get; private set; } = new Dictionary<int, PlugData>();
    // public Dictionary<string, uint> translateIDMap { get; private set; } = new Dictionary<string, uint>();
    public Dictionary<int, TranslateData> translateDataMap { get; private set; } = new Dictionary<int, TranslateData>();
    public Dictionary<int, StateData> stateDataMap { get; private set; } = new Dictionary<int, StateData>();
    public Dictionary<Const.StateConst, PlayerBaseProperty> playerBasePropertyMap { get; private set; } = new Dictionary<Const.StateConst, PlayerBaseProperty>();

    private DataLoader()
    {
        ProtobufReader reader = new ProtobufReader();
        translateDataMap = reader.GenerateDictionary<int, TranslateData>("TranslateData");
        stateDataMap = reader.GenerateDictionary<int, StateData>("StateData");
        playerBasePropertyMap = reader.GenerateDictionary<Const.StateConst, PlayerBaseProperty>("PlayerBaseProperty");
        // 这里可以添加加载数据的逻辑
        // 比如从文件、数据库等加载数据
        AddDictData(cameraData, new CameraData { name = "Z9", price = 12090, uiPath = "img/Z9", priceText = "12090元" });
        AddDictData(cameraData, new CameraData { name = "Z5", price = 9800, uiPath = "img/Z5", priceText = "12090元" });
        AddDictData(cameraData, new CameraData { name = "Z8", price = 10090, uiPath = "img/Z8", priceText = "12090元" });
        AddDictData(cameraData, new CameraData { name = "Z52", price = 12090, uiPath = "img/Z52", priceText = "12090元" });
        AddDictData(cameraData, new CameraData { name = "Z63", price = 12090, uiPath = "img/Z63", priceText = "12090元" });
        AddDictData(cameraData, new CameraData { name = "Z72", price = 12090, uiPath = "img/Z72", priceText = "12090元" });
        AddDictData(cameraData, new CameraData { name = "Z502", price = 12090, uiPath = "img/Z502", priceText = "12090元" });
        AddDictData(cameraData, new CameraData { name = "ZF", price = 12090, uiPath = "img/ZF", priceText = "12090元" });
        AddDictData(cameraData, new CameraData { name = "ZFC", price = 12090, uiPath = "img/ZFC", priceText = "12090元" });

        AddDictData(lensData, new LensData { name = "Z24-120 F4", price = 8000, uiPath = "img/Z24-120", priceText = "8000元", maxAperture = 4.0f, maxFocalLenth = 120, minFocalLenth = 24 });

        AddDictData(plugData, new PlugData { name = "DJI RS4 mini", price = 2000, uiPath = "img/rs4mini", priceText = "2000元" });

    }

    private void AddDictData<T>(Dictionary<int, T> dict, T data) where T : IDDataBase
    {
        dict[data.id] = data;
    }

    public static readonly DataLoader Instance = new DataLoader();

    public static string Tr(string o, int id = 0)
    {

        if (Instance.translateDataMap.TryGetValue(id, out TranslateData trData))
        {

            string result; // 默认返回原始字符串
            switch (G.CurrentLanguage)
            {
                case LocalizationLanguage.English:
                    result = trData.en;
                    break;
                case LocalizationLanguage.Chinese:
                    result = trData.cn;
                    break;
                case LocalizationLanguage.Japanese:
                    result = trData.jp;
                    break;
                default:
                    return o; // 如果没有找到对应的语言，返回原始字符串
            }
            if (result == string.Empty)
            {
                return o; // 如果翻译结果为空，返回原始字符串
            }
            return result;
        }
        return o; // 如果没有找到对应的翻译数据，返回原始字符串
    }
    

}
