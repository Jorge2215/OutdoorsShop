# Malta — Change Password Profile UI

- Date: 2026-05-25T19:08:32.516-03:00
- Owner: Malta

## Decision

Add the password update flow directly to `frontend/src/pages/ProfilePage.tsx` instead of creating a separate route or standalone settings page.

## Why

- The existing profile editor already owns customer account maintenance.
- Reusing the current `Card`, `Input`, `Alert`, and full-width primary `Button` keeps the experience visually consistent.
- Splitting profile and password into separate forms avoids cross-submit side effects while keeping both actions on one page.

## API contract used

- Frontend client added `authApi.changePassword(payload)`.
- Endpoint target: `PUT /api/v1/users/change-password`.
- Payload: `{ currentPassword, newPassword, confirmNewPassword }`.

## UI behavior

- Inline validation covers required fields, minimum 8 characters for the new password, and confirmation matching.
- Password fields clear after a successful change.
- Success and error feedback stay scoped to the password section so profile-save messaging remains independent.
