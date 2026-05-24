# Skill: Azure Functions live testing on Azure (HTTP + timer + queue)

## When to use
Use this pattern when an OutdoorsShop Azure Functions app is already deployed and you need to verify live behavior without changing application code.

## Pattern
1. Read the function source first to confirm trigger types, routes, queue names, cron schedules, and required settings.
2. Probe public HTTP endpoints directly with `curl`.
3. Fetch the Functions master key with Azure CLI and use `/admin/functions` to:
   - list indexed functions
   - manually trigger timer functions
4. Query Application Insights right after each live action to verify the function actually executed.
5. For queue-trigger functions, inspect the storage account behind `AzureWebJobsStorage`:
   - confirm the expected queues exist
   - post a safe test message
   - check whether the message is consumed (`dequeueCount` changes / message disappears)
6. If queue messages are not consumed, treat that as a trigger-path failure even if the function is indexed.
7. Clean up any temporary queues or messages created during testing.

## Commands that worked on 2026-05-24T13:49:18.068-03:00
```powershell
curl.exe -i -s --max-time 15 "https://func-outdoors-dev.azurewebsites.net/api/health"

$masterKey = az functionapp keys list --name func-outdoors-dev --resource-group rg-outdoors-dev --query "masterKey" -o tsv
curl.exe -s "https://func-outdoors-dev.azurewebsites.net/admin/functions" -H "x-functions-key: $masterKey"
curl.exe -X POST "https://func-outdoors-dev.azurewebsites.net/admin/functions/SeasonalDiscount" -H "x-functions-key: $masterKey" -H "Content-Type: application/json" -d "{}"

az monitor app-insights query --app appi-outdoors-dev --resource-group rg-outdoors-dev --analytics-query "union traces, requests | where timestamp > ago(15m) | where tostring(message) has 'SeasonalDiscount' or tostring(name) has 'SeasonalDiscount'"
```

## Failure signals to watch
- `/api/health` fails or times out
- `/admin/functions` does not list expected functions
- timer trigger returns `202` but no matching Application Insights trace appears
- queue trigger messages remain visible with `dequeueCount = 0`
- admin invocation of queue functions returns `400`

## Notes
- Queue names in this repo come from code, not config: `payment-confirmations` and `stock-updates`.
- Successful indexing is not enough; verify end-to-end trigger consumption.
- Prefer safe, non-destructive payloads and always clean up temporary queue artifacts.
