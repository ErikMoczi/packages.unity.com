# Video Module

The Video Module allows you to add auto-play videos in your apps by using a **VideoPlayer** and **VideoSource** component on an Entity and scheduling a **VideoSystem** system.

Tiny Mode supports the video formats commonly supported on the web by most browsers (WebM, MP4, Ogg).

Use cases: 

* Add videos to your game (introduction, final videos in the game, non-interactive cut scenes)

* Add video playable ads.

The **VideoPlayer** component has 4 attributes:

* **Controls**. Set it to true if you want to show the video controls like (play, pause, seeking, volume, fullscreen toggle)

* **Loop**. Set it to true if you want the video to restart automatically when reaching the end

* **currentTime**. A readonly attribute to follow the current playback time of a playing video. It can be useful in cases where you want some actions to happen (like displaying a skip button) after a period of time.

* **Clip**. A link to the video clip entity.

The **VideoClip** component has two attributes:

* **Src**. The source of a video. Formats supported: WebM, MP4, Ogg.

* **VideoClipLoadingStatus**. An enum representing the status of a video. (Unloaded, Loaded, Loading, Loaderror)

The **VideoPlayerAutoDeleteOnEnd** component allows you to automatically delete the video once it reaches the end.

At runtime, when a video entity is added with a video clip, it will automatically play it but the video will be muted by default (this is a requirement from most browsers to avoid noise pollution). 

# How to use it:

- Create a video player entity

- Attach a Video Player component

- Attach a Video Clip component

- Specify a Source to the video clip

Optional

- Attach a **VideoPlayerAutoDeleteOnEnd** component if you want to video to be automatically deleted after it reaches the end

- Attach a **RectTransform** component to position the video on the UI.

_Note: you must schedule the **VideoSystem** system (in UTiny.Video namespace) to play videos._

(See this module's API documentation for more information)