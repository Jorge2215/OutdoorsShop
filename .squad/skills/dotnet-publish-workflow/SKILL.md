Name: dotnet-publish-workflow

Purpose: Guidance for authoring GitHub Actions that publish/push .NET apps to platform-specific App Services.

Pattern:
- If `dotnet publish` includes a runtime identifier (`-r linux-x64`), ensure `dotnet restore` is run with the same runtime (e.g. `dotnet restore --runtime linux-x64`) so runtime-specific assets are present.
- Alternatively avoid runtime-specific restores by publishing without `--no-restore` and let `dotnet publish` perform restore with the correct RID.
- CI tips:
  - Use `actions/setup-dotnet@v4` with `dotnet-version: '10.x'`.
  - Keep builds reproducible by pinning RIDs where necessary and document the platform target in README or workflow comments.

Why: NETSDK1047 (runtime assets missing) is a common failure when restore and publish target different runtimes, causing publish to fail and downstream deploy steps to be skipped.

How to use:
- Add to `.squad/skills/` and refer to it when updating `backend.yml` or similar workflows.
