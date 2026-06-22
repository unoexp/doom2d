
using System;

public static class Utils
{
    public static void OpenWindow(String windowId)
    {
        ServiceLocator.Get<WindowManager>().OpenWindow(windowId);
    }

    public static void CloseWindow(String windowId)
    {
        ServiceLocator.Get<WindowManager>().CloseWindow(windowId);
    }
}