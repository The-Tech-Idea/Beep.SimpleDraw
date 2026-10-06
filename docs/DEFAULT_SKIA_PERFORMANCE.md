# Default catalog performance correction

The first default-catalog release failed the existing ordinary 1,000-object mixed editing gate: median 91.9 ms and p95 108.7 ms. [Failed raw run](evidence/default-skia-performance-run1.json) remains retained. Catalog membership scanned all 249 entries for each object during existing connector/document validation. SkiaCatalog.Find now uses one immutable dictionary built from the same fixed catalog entries, with ordinal type keys. It preserves unknown-type rejection and all existing factories/property/identity validation.

The corrected native candidate is `artifacts/default-skia-catalog-indexed`, offline `8de7c128f221df23`. The complete pipeline passes 542 C# + 77 JavaScript tests, zero managed build/publish warnings/errors, 498 sibling source pins, 56 archives, 151 offline assets and 453 exact ZIP entries. Sibling source is unchanged from the current 504-test run. ZIP SHA-256: `a92a4fb08e48bb1e47ec7634a322b2df52c1361609e022bd16e5728bcf2b42d2`. Compressed inventory: 8,456,092 bytes, within the unchanged 25 MiB budget.

| Ordinary 1,000-object mixed run | Median ms | p95 ms | 50 ms budget |
|---|---:|---:|---|
| Indexed run 1 | 25.1 | 41.3 | Pass |
| Indexed run 2 | 25.0 | 25.6 | Pass |

The same generated fixture, first text selection, fitted viewport, open default catalog and ordinary diagnostic editor were used. Each run completes 140 alternating Home.Shortcut edits with 20 warmup and 120 measured samples, profiling off, at 1280x720/DPR 1 in visible Codex Chromium 154. Final coordinates, object count and selected identity are restored; 140 revisions/dispatches/paints and bounded 100 history entries are checked from the raw report. Six scheduling requests produce exactly two host dispatches and paints without geometry/history changes. The measurement ends on the second animation frame after the real paint callback; it is a presentation proxy, not a GPU timestamp. No builds ran during timings.

[Two raw runs](evidence/default-skia-indexed-performance-repeat.json), [scheduling report](evidence/default-skia-indexed-scheduling.json), [verified summary](evidence/default-skia-indexed-summary.json) and [browser capture](evidence/default-skia-indexed-performance.png) retain evidence. Benchmark native download request status was observed, but completion/file bytes were not captured and are not claimed. Earlier default-catalog UML save/reopen evidence remains scoped to its recorded candidate. This performance gate covers the existing mixed sketch/text board with the palette open; it does not certify all-family board throughput, physical devices, other engines or cold network startup.

The corrected candidate is promoted to [the current preview](http://127.0.0.1:5243/draw/). Full device/host/independent acceptance remains separate. [Catalog guide](DEFAULT_SKIA_CATALOG.md) and [master tracker](../plans/enhancements/MASTER_TRACKER.md) record the continuing plan.
