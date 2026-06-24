mergeInto(LibraryManager.library, {
  UcgWebNotifyEvent: function (eventNamePtr, payloadPtr) {
    var eventName = UTF8ToString(eventNamePtr);
    var payloadText = UTF8ToString(payloadPtr);
    var payload = null;

    try {
      payload = JSON.parse(payloadText);
    } catch (error) {
      payload = { type: eventName, source: "UCG-Battle-Demo" };
    }

    payload.type = payload.type || eventName;

    try {
      if (typeof window !== "undefined" && window.parent && window.parent !== window) {
        window.parent.postMessage(payload, "*");
      }

      if (typeof window !== "undefined" && typeof window.onUnityTutorialComplete === "function") {
        window.onUnityTutorialComplete(payload);
      }

      if (typeof window !== "undefined" && typeof window.dispatchEvent === "function" && typeof CustomEvent === "function") {
        window.dispatchEvent(new CustomEvent(eventName, { detail: payload }));
      }
    } catch (error) {
      if (typeof console !== "undefined" && console.warn) {
        console.warn("UCG WebGL notify failed", error);
      }
    }
  }
});
