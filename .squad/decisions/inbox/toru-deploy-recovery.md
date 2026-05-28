# toru-deploy-recovery

2026-05-28T01:00:14.073-03:00

Decision: Immediate recovery path approved.

Summary:
- Root cause: backend workflow publish failed due to runtime-aware restore mismatch (publish used -r linux-x64 while restore did not include runtime), causing the deploy job not to run and leaving dev App Service on an older build.
- Short-term action (I approve): Perform a manual deploy of the current API publish artifact to app-outdoors-api-dev using Azure CLI. This restores the working API surface quickly and unblocks testing.
- Medium-term action: Cinnamon to update `backend.yml` to make the restore publish sequence runtime-aware (either add `dotnet restore --runtime linux-x64` before publish or remove `--no-restore` on build/publish) and validate via a pushed commit to `dev`.
- Owner: Toru (approve recovery) / Cinnamon (implement workflow fix)

Reasoning:
- Manual deploy is fastest and lowest-risk today; editing workflows requires a small change and verification — leave CI change to Cinnamon to implement and test.

Actions to take now:
1. Build and publish API locally (dotnet publish -c Release -r linux-x64)...
2. Zip and deploy with `az webapp deployment source config-zip --name app-outdoors-api-dev --resource-group rg-outdoors-dev --src publish/api.zip`.
3. Verify Swagger and run any pending EF migrations.

Signed: Toru
