# Phase 10: Export, accessibility, and release hardening

Phase 10 prepares the local-first app for broader testing and release.

## Included

- A data-health check for unknown chapters, duplicates, invalid counters, scores, and review dates.
- An explicit JSON export containing progress, history, review schedule, study goal, and reminder settings.
- Device share-sheet integration; data never leaves the device unless the user chooses a destination.
- Screen-reader headings and descriptive hints on the primary dashboard and data actions.
- Busy-state protection on data operations and non-blocking startup reminder recovery.
- Debug and release build verification plus the full deterministic validation suite.

## Manual release checks

Before store submission, verify speech recognition, text-to-speech, notifications, share-sheet export, large text, dark theme, and screen-reader navigation on physical Android hardware. Signing and store credentials are intentionally outside the source tree.
