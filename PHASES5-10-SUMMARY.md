# Bible Recall Trainer: Phases 5–10

## Phase 5 — Deterministic quizzes

Missing-word quizzes are created only from the canonical chapter text. Generation and answer checking are deterministic and do not alter Bible content.

## Phase 6 — Local progress

Chapter reads, recall attempts, quiz attempts, best scores, and recent practice history are persisted locally with atomic file replacement.

## Phase 7 — Spaced repetition

Completed quiz scores create a local review schedule. Weak results return sooner; strong repeated results increase the interval.

## Phase 8 — Study plan

The dashboard combines a configurable daily chapter goal, syllabus completion, the next unread chapter, and reviews that are currently due.

## Phase 9 — Daily reminders

Android users can opt into one local daily reminder. Notification permission is requested only during explicit setup, and turning the feature off cancels the alarm.

## Phase 10 — Release hardening

Users can check the consistency of their saved data and explicitly export a readable JSON backup through the device share sheet. Primary screens include screen-reader headings and action hints. Automated validation covers canonical content, media-session behavior, recall, quizzes, persistence, review scheduling, planning, reminders, integrity checks, and export structure.

## Privacy boundary

The app remains local-first. It has no account system, analytics, advertising, cloud synchronization, or remote AI dependency. Bible content is bundled for offline use, and learning data leaves the device only through an export the user initiates.
