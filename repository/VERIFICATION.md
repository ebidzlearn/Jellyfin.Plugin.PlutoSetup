# Packaging Verification

- [x] Release ZIP exists: dist/plutotvautotuner_0.1.0.0.zip
- [x] Repository ZIP exists: repository/releases/plutotvautotuner_0.1.0.0.zip
- [x] Repository artwork exists: repository/images/pluto-tv-auto-tuner.png
- [x] GitHub Pages docs mirror contains matching manifest, ZIP, and artwork
- [x] Compiled assembly version matches project version: 0.1.0.0
- [x] ZIP contains only runtime plugin payload files: Jellyfin.Plugin.PlutoSetup.dll, meta.json, LICENSE, and pluto-tv-auto-tuner.png
- [x] ZIP excludes source, obj, bin, PDB, XML docs, deps files, scripts, git files, secrets, and user-specific settings
- [x] Manifest root is a JSON array
- [x] Manifest GUID matches compiled plugin GUID: 0d7f2f32-8b2d-4d3f-b6c4-90c5a0b49f1b
- [x] Manifest version matches project and ZIP filename: 0.1.0.0
- [x] Manifest targetAbi is Jellyfin ABI 10.11.0.0
- [x] Manifest sourceUrl is absolute HTTP/HTTPS: https://raw.githubusercontent.com/ebidzlearn/Jellyfin.Plugin.PlutoSetup/main/docs/releases/plutotvautotuner_0.1.0.0.zip
- [x] Manifest imageUrl is absolute HTTP/HTTPS: https://raw.githubusercontent.com/ebidzlearn/Jellyfin.Plugin.PlutoSetup/main/docs/images/pluto-tv-auto-tuner.png
- [x] Manifest checksum matches final ZIP MD5: b8b5a38729ee433e546e62c66832093d
- [x] Catalog visibility check by manifest data: plugin entry name is Pluto TV Auto Tuner

To prove this inside a running Jellyfin server, host this folder and add:

https://raw.githubusercontent.com/ebidzlearn/Jellyfin.Plugin.PlutoSetup/main/docs/manifest.json

Then open Dashboard > Plugins > Catalog and verify Pluto TV Auto Tuner appears.