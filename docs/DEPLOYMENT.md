# Static deployment and operation
Date: 2026-10-05. [Build contract](BUILD_REPRODUCIBILITY.md) · [Verified desktop scope](P6_BUILD_HOST_VERIFICATION.md).

## Prepare the portable artifact
Build once for the intended mount: `./scripts/build.ps1 -Output artifacts/candidate -BasePath /draw/`, or omit BasePath for `/`. Use the verified `publish/wwwroot` folder or `simpledraw-static.zip`, with its SHA256, asset report and provenance. Never deploy the managed-build intermediate folder. The host needs ordinary static-file service; users need a supported browser. No .NET runtime, Node service, database, package feed or Beep.Skia checkout is required in production.

Serve over HTTPS in production; loopback HTTP is supported for development. `file://` cannot supply the WASM/service-worker contract. Mount the entire folder at the exact report base path. Keep slash termination and case consistent; redirect `/draw` to `/draw/`. Changing a mount requires a newly finalized/verified candidate before packaging, including its entry integrity and encodings. Do not edit `index.html`, scripts or compressed variants in place after verification.

For a local verified preview:

```powershell
node scripts/serve-static.mjs artifacts/candidate/publish/wwwroot artifacts/candidate/asset-report.json 5210
# Or use the wrapper with the same verified folder/report:
./scripts/serve-published.ps1 -ReleasePath artifacts/candidate/publish/wwwroot -Port 5210
```

The Node helper binds only 127.0.0.1 and verifies the artifact before starting. Its actual HTTP/root/subpath/offline/update behavior has been exercised. It is a reference contract for a production static host; no IIS/nginx/cloud recipe or public deployment has been validated here.

## Static host contract
| Resource/behavior | Required handling |
|---|---|
| WASM | `Content-Type: application/wasm` |
| JavaScript | `text/javascript`; serve actual files, not SPA HTML for a missing asset |
| Font/manifest | Correct font MIME and `application/manifest+json` |
| Brotli/gzip | Negotiate an available variant with its Content-Encoding; send original MIME and `Vary: Accept-Encoding`; identity remains available |
| Fingerprinted assets | Long immutable cache; the reference uses one year |
| Entry, worker, offline manifest and other unfingerprinted assets | Revalidate (`Cache-Control: no-cache`); avoid a stale entry/worker |
| CSP | Send the exact `csp` from that artifact's asset report; entry hashes change per candidate |
| Other headers | `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer`, `Cross-Origin-Opener-Policy: same-origin` |
| SPA path | Resolve only app navigation paths to index; missing dotted assets return 404; stay inside the declared mount |
| HTTP methods/paths | Static GET/HEAD; reject traversal/encoded separators/write methods |

The worker scope/start URL are relative to the build base. Assets and fonts are self-hosted. Do not relax the script CSP to general unsafe-eval to hide a deployment error. The report's required WASM token and exact inline hashes support this build. The reference allows inline styles for existing editor layout; its policy is recorded with the artifact.

## Promote and update
Retain the previous compatible candidate and provenance. Upload a complete new candidate to a staging location, verify file hashes/ZIP entries, and promote the mount atomically; do not expose a mixture of old entry/manifest and new assets. On the intended HTTPS host, verify startup, font/native paint, native/PNG/SVG downloads, reload, stopped-network offline reload/edit/save, worker update and multi-tab refusal before approval. Record host/browser/version and actual results in the release checklist.

Updates wait while editing. Finish/cancel a draft or gesture, wait for saved status, and close other editor tabs. Then use Update app. The app reloads on controller activation and restores its board. A second editor can use a file-only snapshot but cannot overwrite the origin/base-path writer. A failed update should retain the prior installed worker; investigate missing/mismatched resources rather than clear user storage.

## Documents, recovery and rollback
Browser autosave is scoped to origin/base path. Moving domains or mount paths does not move those drawings or libraries. Have users download native drawings and libraries before a move. Browser storage can be evicted; native downloads are the portable backup. PNG/SVG are presentation exports and cannot restore editable objects.

If storage fails, the board stays pending and native download remains available. Retry after resolving quota/permission. Unreadable stored bytes are downloadable before explicit recovery replacement. Close other writers before retrying. Do not erase storage as an operator shortcut.

Rollback only to a candidate that understands existing native/autosave envelopes and library schema. Current drawing envelope is v6/Beep schema 3; older releases may reject it. Keep backups before a schema-changing release. Re-promoting a previous folder is not proof of safe data downgrade; verify its decoder/update behavior first. Offline users may retain an installed candidate until an approved update is offered.

## Status and limitations
For controlled local startup evidence, use `scripts/serve-profiled.mjs` with a declared download cap/response delay, a previously unused origin and an absent capture file. It delegates to the same verified host and records aggregate encoded-body transfers. [Performance protocol](P6_PERFORMANCE.md) includes three 50 Mbps / 40 ms runs and exact limitations. This is verification tooling, not a production hosting requirement or physical-network certification.

Local pinned Release reproduction and desktop root/subpath/offline/update checks passed. Public HTTPS deployment, hosted CI, installed-app/device/browser matrix, measured performance and independent acceptance remain separate release gates. The artifact reports are evidence, not a release signature.
