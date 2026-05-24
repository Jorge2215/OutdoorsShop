# Skill: Azure Functions deploy on Flex Consumption (.NET 10, Linux)

## When to use
Use this pattern when an OutdoorsShop Azure Functions app targets `.NET 10` isolated on Linux. Classic Linux Consumption (`Y1`) is not a supported host for this combination; use **Flex Consumption**.

## Deployment pattern
1. Provision the Function App on **Flex Consumption** (`FC1`) in the target region.
2. Keep `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` and target `.NET 10`.
3. Publish for Linux:
   ```bash
   dotnet publish src/OutdoorsShop.Functions/OutdoorsShop.Functions.csproj -c Release -r linux-x64 --self-contained false /p:UseAppHost=false --output publish/functions
   ```
4. Package from the publish directory root so hidden generated assets are included:
   ```bash
   cd publish/functions
   zip -r ../functions.zip .
   ```
   On Windows, `tar -a -cf <zip> .` from the publish directory also works.
5. Deploy with Azure CLI:
   ```bash
   az functionapp deployment source config-zip --name <app-name> --resource-group <rg> --src publish/functions.zip --timeout 600
   ```

## Critical detail
Flex zip deployment validates package structure. The archive **must** contain the generated `.azurefunctions/` folder at the zip root. Archives created from wildcard-only inputs can omit hidden folders and fail validation.

## Verification
- `az functionapp function list --name <app-name> --resource-group <rg>` should list all functions.
- `GET /api/health` should return `200 {"status":"ok"}`.
- Root hostname should stop returning `503`.
