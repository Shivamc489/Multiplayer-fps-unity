using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneSignalManager : MonoBehaviour {

    // Enable line below to enable logging if you are having issues setting up OneSignal. (logLevel, visualLogLevel)
    // OneSignal.SetLogLevel(OneSignal.LOG_LEVEL.INFO, OneSignal.LOG_LEVEL.INFO);
    private void Start()
    {
        OneSignal.StartInit("e60be673-5c92-4d76-a3fc-030ab3538ac5")
    .HandleNotificationOpened(HandleNotificationOpened)
    .EndInit();

        OneSignal.inFocusDisplayType = OneSignal.OSInFocusDisplayOption.Notification;
    }
    


// Gets called when the player opens the notification.
private static void HandleNotificationOpened(OSNotificationOpenedResult result)
{
}
}
