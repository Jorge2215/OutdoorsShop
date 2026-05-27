# Creta history (summary)

Recent activity summary:
- Added 7 change-password integration tests (happy path, wrong password, unauthenticated, validation scenarios, security checks).
- Noted test failures (6 contract tests 404) due to backend route mismatch; recommended Cinnamon align route to /api/v1/users/change-password.
- Fixed TestWebAppFactory bootstrap for Identity tables.
- Continued routine E2E and journey testing results archived.
- Reviewed async order receipts on 2026-05-27T16:51:34.303-03:00, added receipt endpoint integration coverage (401/200/403/404), and added receipt HTML encoding coverage for XSS-sensitive fields.
