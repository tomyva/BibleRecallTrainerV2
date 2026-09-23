# Phase 7: Spaced repetition

Completed chapter quizzes update a local chapter-level review schedule in `review-schedule.json`.

- Below 70%: review again in 1 day and reset the success streak.
- 70–89%: increase the previous interval by 1 day.
- 90–100%: double the previous interval, with a minimum of 3 days.

The calculation is deterministic and transparent. Review data remains local and separate from canonical Bible content. Voice-recall attempts do not reschedule an entire chapter because they cover only one verse.
