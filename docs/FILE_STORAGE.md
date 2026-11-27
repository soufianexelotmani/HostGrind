# Private property files

Property file routes use `/api/v1/organizations/{organizationId}/properties/{propertyId}`. The `photos` and `documents` collections accept multipart POSTs with one `file` field and expose listings by GET. A GET or DELETE with a file link ID streams or archives that association. `PUT photos/order` accepts the complete list of active photo link IDs in display order and the current property version.

## Storage and configuration

SQL Server stores the `StoredFile` metadata, including an opaque key, sanitized display name, media type, byte count, SHA-256 digest, upload time, uploader and scan state. `PropertyPhoto` and `PropertyDocument` link each file to its property. The local provider stages new files in a private directory and renames them to their final key. API responses expose association IDs, display metadata and readability. Physical paths and storage keys stay in the backend.

| Environment variable | Default and behavior |
|---|---|
| `PropertyStorage__Root` | Development defaults to `private-files` beside the running API binary. Set an absolute, private, persistent directory for deployed environments. The app rejects directories under the web root and paths with linked directories. |
| `PropertyStorage__PhotoBytes` | 10,485,760 bytes (10 MiB). |
| `PropertyStorage__DocumentBytes` | 20,971,520 bytes (20 MiB). |
| `PropertyStorage__AllowUnscannedInDevelopment` | Defaults to true for development previews. Set false to exercise quarantine without the preview exception. |

Each size setting accepts 1 to 60,000,000 bytes. The upload action caps the request at the configured value plus 64 KiB of multipart overhead and checks the file size again. Align any proxy and hosting body limits with these settings. Restrict directory access to the API process and authorized operators. Keep the directory out of static-file mappings, public web roots and public backups.

Photos accept JPEG (`.jpg` and `.jpeg`), PNG and WebP. The API checks extension, signature, container boundary and complete image decode. The decoder accepts single-frame photos up to 24 million pixels and runs at most two decodes at a time per API process. The signed-in browser uses an authenticated request and a temporary object URL for previews. PDF uses `.pdf` and a `%PDF-` signature. This identifies the PDF container and does not validate its internal structure or safety. SkiaSharp is pinned in the lock files with Windows and Linux native assets. [SkiaSharp license](https://github.com/mono/SkiaSharp/blob/main/LICENSE.md)

## Access and lifecycle

- Managers manage files within their organization. A Super Admin uses the explicit organization route. Owners can list and download readable files on their own properties. File routes resolve the property and check that the link belongs to it.
- Download responses carry `Cache-Control: private, no-store` and `X-Content-Type-Options: nosniff`. Photos use their validated media type. PDFs use `Content-Disposition: attachment` with the sanitized filename. Exclude these API routes and private responses from any future Angular service worker cache. The frontend host CSP needs `blob:` in `img-src` for previews.
- The first readable photo in persisted order supplies the cover. The property lock serializes photo upload, ordering, removal, property status changes and document attachment. Ordering requires a current rowversion, so competing writes with the same version reject the stale request.
- Archive hides the association and keeps its SQL row and bytes. An archived property is read-only for file changes. A document linked to a contract cannot be archived. The attachment lookup selects an active, readable document in the same property under the existing contract lock.
- The registered fallback scanner keeps files quarantined. Production downloads and covers require clean files. Development can view quarantined files with the configured exception. A rejected scan refuses the upload. Production requires a real scanner and a process that records results for quarantined files. Antivirus effectiveness and scanner deployment remain open.

## Failure handling and backups

A failed write leaves no committed SQL metadata. The local provider removes incomplete staging files when possible. A failed metadata write triggers blob cleanup. An uncertain commit retains the bytes so a committed row can still be downloaded. The read-only inventory reports unmatched files older than 24 hours, stale staging files and SQL records with missing local blobs. It includes archived files in its reference check.

```powershell
dotnet run --project backend -- --inventory-private-files
```

Run from a trusted terminal against the intended SQL catalog and its matching storage root. Pause file writes during reconciliation. Check both sides and the latest backups before taking manual action. The command reports opaque identifiers and performs no deletion. Back up SQL and the private directory together with access controls and a coordinated recovery point. Periodically test restoration, including downloads of documents retained by contracts.

An Azure provider can later implement the existing stream-based `IFileStorageService` contract using private objects and the same opaque SQL keys. HostGrid retains HTTP authorization and SQL metadata. A remote storage provider would supply its own inventory.
