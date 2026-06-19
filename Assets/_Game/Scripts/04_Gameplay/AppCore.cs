using UnityEngine;


public class AppCore
{
    private static AppCore _instance;

    public static AppCore Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AppCore();
            }
            return _instance;
        }
    }

    public int targetFrameRate {
        get => Application.targetFrameRate;
        set => Application.targetFrameRate = value;
    }
}