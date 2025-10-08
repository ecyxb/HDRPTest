using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface IDataBase { }
public abstract class IDDataBase { public int id; }


public class TrData : IDataBase
{
    public int id;
    public string cn;
    public string en;
    public string jp;
}

public class CameraData : IDDataBase
{

    public string name;
    public int price;
    public string uiPath;
    public string priceText;
}

public class LensData : IDDataBase
{
    public string name;
    public int price;
    public string uiPath;
    public string priceText;
    public int minFocalLenth;
    public float maxAperture;
    public int maxFocalLenth;

}

public class PlugData : IDDataBase
{
    public string name;
    public int price;
    public string uiPath;
    public string priceText;
    public int plugType;

}
