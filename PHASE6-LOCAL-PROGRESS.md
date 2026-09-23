# Phase 6: Local progress and history

Learning activity is stored in `learning-progress.json` under the app's private data directory. Bible content remains packaged, read-only, and separate.

The progress ledger records chapter reads, revealed voice-recall comparisons, completed quizzes, best quiz percentages, last-study times, and a bounded activity history. Writes use a temporary file followed by replacement so an interrupted write is less likely to damage existing data.

Progress never leaves the device. A failure to load or save progress is reported without preventing Scripture reading or practice.
