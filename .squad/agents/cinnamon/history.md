# history — Cinnamon (summarized)

Recent highlights (summary):

- Implemented async order receipts: added ReceiptGenerationMessage contract, IReceiptQueuePublisher, PaymentConfirmationFunction as producer, and ReceiptGenerationFunction writing deterministic HTML receipts to `order-receipts` container.
- Exposed `GET /api/v1/orders/{id}/receipt` returning availability and short-lived SAS URL when present.
- CI validation: API and Functions tests added/updated for receipt endpoint and HTML encoding; build/tests reported green in recent runs.

Full chronological history archived to: history-archive-20260527T195134Z.md
