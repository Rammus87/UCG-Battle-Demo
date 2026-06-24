using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace UCG
{
    public static class UcgWebBridge
    {
        public const string TutorialCompleteEventName = "ucgTutorialComplete";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void UcgWebNotifyEvent(string eventName, string payloadJson);
#endif

        public static void NotifyTutorialComplete()
        {
            string payloadJson = "{\"type\":\"" + TutorialCompleteEventName + "\",\"source\":\"UCG-Battle-Demo\"}";

#if UNITY_WEBGL && !UNITY_EDITOR
            UcgWebNotifyEvent(TutorialCompleteEventName, payloadJson);
#else
            Debug.Log($"UCG Web callback skipped outside WebGL: event={TutorialCompleteEventName}, payload={payloadJson}");
#endif
        }
    }
}
