using System;
using UnityEngine;


[Serializable]
public class AircraftRecord
{
    public string Name;
    public string PrimaryRole;
    public string Manufacturer;
    public string Country;

    public float Number;       
    public float ActiveSince;  
    public float LastBuilt;    
    public float Retired;      

    public string State;       

    public int Crew;         
    public float Length;       
    public float Wingspan;     
    public float Height;       
    public float WingArea;     
    public float MaxSpeed;     
}

public static class JsonArrayHelper
{
    [Serializable]
    private class Wrapper<T>
    {
        public T[] items;
    }

    public static T[] FromJson<T>(string json)
    {
        string wrapped = "{\"items\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
        return wrapper.items;
    }
}
