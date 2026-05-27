# history — Cinnamon (summarized)

Recent highlights (summary):

- Implemented async order receipts: added ReceiptGenerationMessage contract, IReceiptQueuePublisher, PaymentConfirmationFunction as producer, and ReceiptGenerationFunction writing deterministic HTML receipts to `order-receipts` container.
- Exposed `GET /api/v1/orders/{id}/receipt` returning availability and short-lived SAS URL when present.
- CI validation: API and Functions tests added/updated for receipt endpoint and HTML encoding; build/tests reported green in recent runs.

Full chronological history archived to: history-archive-20260527T195134Z.md

2026-05-27T20:27:02Z - scribe: merged inbox entries into .squad/decisions.md (
  - cinnamon-azure-deploy-readiness.md
  - toru-azure-deploy-readiness.md
)

## 2026-05-27T20:47:27Z — scribe update
- Merged 1 inbox items into decisions.md
- Archived 0 entries (none older than cutoff)

