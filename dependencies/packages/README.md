# Pinned owner package inputs
These two exact The Tech Idea packages are required by the existing Beep.Skia project. Their versions are not on nuget.org; they are bundled so a clean build does not depend on an undeclared machine feed or global package cache. Both nuspecs declare MIT. They remain build inputs only; the deployable application contains its trimmed runtime dependencies.

- TheTechIdea.Beep.DataManagementModels 3.1.1: source https://github.com/The-Tech-Idea/BeepDM at f305eaf64037392fa4b907aa69f64f5d641c69d4. SHA256 5a9ac5f7fbf9581d99a7ce871ddb36ff62f6b0b79fe6ec110dce4d75aa52f46f.
- TheTechIdea.Beep.Vis.Modules 2.0.44: source https://github.com/The-Tech-Idea/BeepEnterprize.winform at 9b62321ff640ebcc8bb6cca1f83e675a29a92215. SHA256 0e2c8c5809846dca31b6fa3c07f6487165337e3e21a002d0e60124731c13e718.

NuGet.Config maps only these exact IDs to this folder; public packages use nuget.org. Dependency locks additionally pin NuGet content hashes. Package hashes/provenance must be checked before restore; replacing a binary requires an explicit input-lock update and verification. These packages are not claimed to be published to a public feed.
