using UnityEditor;
using UnityEngine;

public class ScreenshotEditorScene : MonoBehaviour
{
    // Add a menu item named "Do Something" to MyMenu in the menu bar.
    [MenuItem("Scene/Screenshot")]
    static void Screenshot()
    {
        ScreenCapture.CaptureScreenshot(System.DateTime.Now.ToString("MM-dd-yy (HH-mm-ss)") + ".png", 1);
    }
}