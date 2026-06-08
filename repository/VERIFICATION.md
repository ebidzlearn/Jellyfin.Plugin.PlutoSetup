# Packaging Verification

- [x] Release ZIP exists: dist/plutotvautotuner_0.1.0.3.zip
- [x] Repository ZIP exists: repository/releases/plutotvautotuner_0.1.0.3.zip
- [x] Repository artwork exists: repository/images/pluto-tv-auto-tuner.png
- [x] GitHub Pages docs mirror contains matching manifest, ZIP, and artwork
- [x] Compiled assembly version matches project version: 0.1.0.3
- [x] ZIP contains only runtime plugin payload files: Jellyfin.Plugin.PlutoSetup.dll, meta.json, LICENSE, and pluto-tv-auto-tuner.png
- [x] ZIP excludes source, obj, bin, PDB, XML docs, deps files, scripts, git files, secrets, and user-specific settings
- [x] Manifest root is a JSON array
- [x] Manifest GUID matches compiled plugin GUID: 0d7f2f32-8b2d-4d3f-b6c4-90c5a0b49f1b
- [x] Manifest version matches project and ZIP filename: 0.1.0.3
- [x] Manifest targetAbi is Jellyfin ABI 10.11.0.0
- [x] Manifest sourceUrl is absolute HTTP/HTTPS: https://ebidzlearn.github.io/Jellyfin.Plugin.PlutoSetup/releases/plutotvautotuner_0.1.0.3.zip
- [x] Manifest imageUrl is absolute HTTP/HTTPS: https://ebidzlearn.github.io/Jellyfin.Plugin.PlutoSetup/images/pluto-tv-auto-tuner.png
- [x] Manifest checksum matches final ZIP MD5: 0771124bfc62612c672b83be8396371b
- [x] Catalog visibility check by manifest data: plugin entry name is Pluto TV Auto Tuner

To prove this inside a running Jellyfin server, host this folder and add:

https://ebidzlearn.github.io/Jellyfin.Plugin.PlutoSetup/manifest.json

Then open Dashboard > Plugins > Catalog and verify Pluto TV Auto Tuner appears.