// Umami tracking bridge — exposes TrackUmami() to Unity so C# code can
// call the browser-side umami.track(event, data) API. Safe on any domain:
// if umami hasn't loaded we silently skip.
mergeInto(LibraryManager.library, {
  TrackUmami: function (namePtr, jsonPtr) {
    try {
      var name = UTF8ToString(namePtr);
      var json = UTF8ToString(jsonPtr);
      var data = {};
      if (json && json.length) {
        try { data = JSON.parse(json); } catch (e) { data = { raw: json }; }
      }
      if (window.umami && typeof window.umami.track === 'function') {
        window.umami.track(name, data);
      }
    } catch (e) {
      console.warn('[UmamiBridge] track failed:', e);
    }
  }
});
