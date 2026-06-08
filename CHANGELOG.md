# Changelog

## 0.1.0.3

- Moved dashboard action buttons under their related fields instead of grouping them at the bottom of the page.
- Placed M3U copy under the generated M3U preview, XMLTV copy and validation under the generated XMLTV preview, and Docker actions under the Docker command field.

## 0.1.0.2

- Fixed the dashboard page reading Jellyfin plugin API responses with PascalCase JSON property names.
- Restored generated URL previews, status values, validation alerts, and Docker command previews in Jellyfin's plugin UI.
- Removed unavailable native generation/login action buttons and made auto-add visibly unavailable in the MVP UI.

## 0.1.0.1

- Added original Pluto TV Auto Tuner artwork to the hosted catalog manifest.
- Bundled the artwork in the compiled plugin ZIP for installed plugin metadata.
- Mirrored hosted repository files into the GitHub Pages `docs` folder.

## 0.1.0.0

- Added hosted no-Docker MVP mode using configurable third-party M3U/XMLTV URLs.
- Added bounded streaming URL validation for M3U and XMLTV resources.
- Added optional Docker helper mode for jonmaddox/pluto-for-channels command and URL generation.
- Added admin-only plugin API endpoints and dashboard configuration page.
- Added native no-Docker mode as unavailable by default with clear 503 endpoint responses.
- Added manual Jellyfin Live TV setup fallback and auto-add availability reporting.
