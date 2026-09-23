# Phase 3: Text-to-Speech

The Chapter Reader uses .NET MAUI's native `ITextToSpeech` API through the app's `ISpeechService`. A separate `ChapterSpeechPlayer` reads canonical Bible verses sequentially, handles cancellation, and reports the active verse without changing the Bible content.

## Reader behavior

- **Read** starts at verse 1 and reads the chapter verse by verse.
- **Stop**, leaving the reader, changing chapters, or app deactivation cancels playback.
- The active verse is highlighted and scrolled into view.
- Verse numbers are displayed but are not included in the spoken text.
- Previous and next chapter navigation never starts playback automatically.

## Settings

The reader lists English locales supplied by the device and supports voice/locale selection, rate, and pitch. Settings are saved with .NET MAUI Preferences. If a saved voice is no longer installed, the first available English voice is selected and saved as the fallback.

The default rate and pitch are both `1.0`. Rate is constrained to `0.1–2.0`; pitch is constrained to `0.0–2.0`.

## Offline and platform notes

Bible text remains fully offline. Speech can also work offline when the device's selected text-to-speech engine has suitable English voice data installed. Some device engines may download a voice the first time it is selected; that behavior is controlled by the operating system, not this app.

Android 11 and later require the `android.intent.action.TTS_SERVICE` query included in `AndroidManifest.xml`. No additional setup is required for iOS, Mac Catalyst, or Windows. Background speech is not supported; playback stops when the app is deactivated or stopped.

If no English voice is available or the native engine fails, the reader remains usable and displays a speech-specific message.
