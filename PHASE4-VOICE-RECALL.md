# Phase 4: Voice Recall

Voice Recall lets the user practice one verse at a time from the current chapter. The canonical World English Bible text remains unchanged and separate from all recognition results.

## Speech recognition

The app uses `CommunityToolkit.Maui` 13.0.0 and its online `ISpeechToText` implementation behind the app-owned `ISpeechRecognitionService` interface. This toolkit version supports .NET 10 and the installed MAUI 10.0.20 packages.

Recognition uses English (`en-US`) and reports partial text while listening. Only one recognition session can hold the microphone. Starting a replacement session cancels the old session and waits for its cleanup, preventing overlap and late results from reaching a new verse.

The user can stop and process a result or cancel it. Recognition is also cancelled when changing verses, returning to the Chapter Reader, leaving the recall page, or when the application is deactivated.

## Permissions

Permissions are requested only when **Start Recall** is selected.

- Android: `RECORD_AUDIO` plus speech-recognition service discovery in `AndroidManifest.xml`.
- iOS and Mac Catalyst: microphone and speech-recognition usage descriptions in `Info.plist`.
- Windows: microphone device capability in `Package.appxmanifest`.

When permission is denied, the app leaves reading and Text-to-Speech available and offers a link to application settings.

## Recall workflow

1. Open a chapter and choose **Practice Voice Recall**.
2. Read or listen to the current verse.
3. Hide the verse.
4. Start recall and recite it.
5. Stop, then review the recognized transcript.
6. Reveal and compare the transcript with the canonical verse.
7. Try again or move to another verse.

Text-to-Speech and recognition never intentionally run together. Starting either operation cancels the other media session.

## Comparison rules

`RecallComparisonService` performs a local word-level edit-distance comparison. It ignores case and common punctuation and normalizes straight/typographic apostrophes. It identifies matched, missing, extra, and substituted words.

The original canonical verse and transcript are preserved for display. The percentage is a basic word-level match only; it does not measure meaning, understanding, or pronunciation.

## Privacy and connectivity

Bible Recall Trainer does not save microphone audio and does not send audio or transcripts to an application-controlled backend. The selected device speech provider may process recognition online. The Community Toolkit's online recognizer therefore may require internet access. Offline recognition is not promised in this phase.

Transcripts exist only in the active recall screen and are cleared when the user changes verses or requests another attempt.

## Validation

The existing validation executable now also checks comparison normalization, exact/missing/extra/different words, empty input, replacement recognition sessions, Stop, Cancel, overlap prevention, stale results, and recognizer failures.
