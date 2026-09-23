# Bible content provenance

The bundled Scripture text is the **World English Bible (WEB), 2020 stable text edition**, a public-domain translation.

- Publisher/source: eBible.org
- Translation page: https://ebible.org/details.php?id=engwebp
- Source archive: https://ebible.org/Scriptures/engwebp_vpl.zip
- Source format: verse-per-line XML (`engwebp_vpl.xml`)
- Retrieved: 2026-09-19
- License: Public Domain

Only the application syllabus was extracted: Ruth 1–4, 1 Samuel 1–7, Ecclesiastes 1–6, John 1–12, and Galatians 1–6. Verse wording and punctuation come from the source archive; source-format trailing separator whitespace was removed when producing JSON.

`syllabus.json` is the authoritative ordered syllabus and translation metadata. Each book has its own JSON file so canonical verse text remains separate from application logic and any future learning metadata.

Run the repeatable content validation from the repository root with:

```text
dotnet run --project BibleRecallTrainerV2.DataValidation/BibleRecallTrainerV2.DataValidation.csproj
```
