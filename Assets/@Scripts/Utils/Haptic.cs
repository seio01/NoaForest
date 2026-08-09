using UnityEngine;

public static class Haptic
{
    public static bool Enabled => Managers.UserSetting.CurrentSetting.IsVibrationEnabled;

    public static void Vibrate()
    {
        if (!Enabled)
            return;

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }
}
