# Email Features Roadmap

Parent issue: [#169 — Integrate email service](https://github.com/BartekBanas/BandFounder/issues/169)

Sub-issues:

- [#170 — Setup emailing service](https://github.com/BartekBanas/BandFounder/issues/170)
- [#90 — Resetting password through email](https://github.com/BartekBanas/BandFounder/issues/90)
- [#171 — Email notifications for incoming message](https://github.com/BartekBanas/BandFounder/issues/171)

## Locked decisions

| Decision | Choice |
|---|---|
| Provider | Resend (API key in `BandFounder.Api/.env`) |
| Password-reset token TTL | 15 minutes |
| `EmailOnNewMessage` preferences | Default ON with an account-settings toggle |
| Message email delay | User-configurable: 5 minutes, 1 hour, or 1 day; default 1 day |
| Background worker | EF outbox table + `BackgroundService` (no Hangfire) |
| Scope for now | `#170` foundation, `#90` password reset, and `#171` message notifications |

### Message notification worker configuration

The worker supports these environment-variable overrides:

- `MESSAGE_EMAIL_POLL_INTERVAL_SECONDS` — polling interval, default `60`
- `MESSAGE_EMAIL_MAX_ATTEMPTS` — delivery attempts before `Failed`, default `3`

`MaxSnippetLength` and `StaleClaimMinutes` are configured in the
`MessageEmailNotifications` application-configuration section. Delay is always selected per account and is never
configured globally.

## Assessment

The parent issue is a reasonable dependency grouping: build email delivery first, then password reset, then message
notifications. Message-email notifications now use durable unread state, account preferences, and queued delivery rather
than sending an email inline for every message.

## Recommended issue structure and order

1. **#170 Email foundation** — provider-independent sending, configuration, templates, and testability.
2. **#90 Password reset** — the first end-to-end consumer because its behavior is bounded and security requirements are
   clear.
3. **#171 Message notifications** — implement only after defining unread state and notification preferences; split this
   issue into smaller children if possible.

## 1. Build a safe email foundation

- Define an application-level `IEmailSender` abstraction and typed message/template models; put the SMTP or
  transactional-provider implementation in Infrastructure and register it in [
  `Backend/BandFounder/BandFounder.Api/Program.cs`](../Backend/BandFounder/BandFounder.Api/Program.cs).
- Load sender address, provider credentials, and frontend base URL from environment variables or .NET User Secrets.
  Never commit or copy `emailCredentials.json` into build output; add it to ignore rules and rotate the credential if it
  has ever been exposed.
- Support HTML plus plain-text content, cancellation, structured logging, and a fake sender for tests. Keep
  provider-specific exceptions behind the abstraction.
- Establish database migrations before adding token/read-state tables; the current `Database.EnsureCreated()` in [
  `Backend/BandFounder/BandFounder.Api/Program.cs`](../Backend/BandFounder/BandFounder.Api/Program.cs) does not evolve
  an existing schema reliably.

## 2. Implement password reset securely

- Add a `PasswordResetToken` entity containing account ID, a **hash of a cryptographically random opaque token**,
  expiry, and consumed timestamp. Do not reuse login JWTs.
- Extend [
  `Backend/BandFounder/BandFounder.Application/Services/AccountService.cs`](../Backend/BandFounder/BandFounder.Application/Services/AccountService.cs)
  and [
  `Backend/BandFounder/BandFounder.Api/Controllers/AccountController.cs`](../Backend/BandFounder/BandFounder.Api/Controllers/AccountController.cs)
  with request-reset and complete-reset flows.
- The request endpoint must always return the same response whether the email exists, be rate-limited, invalidate older
  active tokens, and send a short-lived HTTPS reset link. The completion endpoint validates once, hashes the new
  password through the existing hashing service, consumes the token atomically, and rejects reuse/expiry.
- Add frontend API calls, `/forgot-password` and `/reset-password` routes, forms, and a login-page link. Cover
  unknown-email behavior, token expiry/reuse, successful reset, and email-link generation with tests.

## 3. Model notification intent before sending mail

- Per-user `EmailOnNewMessage` and `EmailUnreadDelayMinutes` preferences are exposed through account settings.
- The existing per-member `ChatroomReadState.LastReadAt` and authenticated mark-read endpoint provide durable unread
  state for delivery decisions.

## 4. Deliver useful, non-spammy message notifications

- After a message is successfully persisted in `MessageService.SendMessage`, publish/enqueue one notification candidate
  per recipient except the sender; do not send SMTP inline in the HTTP request.
- Process candidates in a background worker with durable storage/outbox semantics. Delay and deduplicate by
  recipient/chatroom, using the recipient's configured 5-minute, 1-hour, or 1-day delay, then send only if the
  conversation is still unread and email notifications remain enabled.
- Send one summary email with sender/chat name, a safe text snippet, and a configurable deep link to
  `/messages/{chatRoomId}`. Escape user-generated content and avoid placing full sensitive messages in email by default.
- Test recipient selection, sender exclusion, preferences, read-before-delay suppression, grouping, retries, and
  idempotency. Treat authenticated WebSocket presence as a later optimization; the current WebSocket path runs before
  authentication and tracks chatrooms rather than users.
- Delivery is at-least-once if the process crashes after the provider accepts an email but before the outbox row is
  marked `Sent`; the outbox identity is retained for future provider idempotency support.

## Definition of done

- Email secrets are externalized and no real credentials are committed.
- Password reset is enumeration-resistant, rate-limited, expiring, single-use, and tested end to end.
- Message emails are optional, delayed/grouped, based on durable unread state, and cannot block message posting.
- Provider failures are observable and retryable; delivery is at-least-once and may duplicate on the
  crash-after-accept window described in section 4.

## Implementation todos

- [x] Implement provider-independent email delivery, externalized configuration, templates, tests, and migration
  readiness.
- [x] Add secure opaque reset tokens, backend endpoints, frontend flows, rate limits, and end-to-end tests.
- [x] Add email preferences and durable per-chat read state with account and messaging APIs.
- [x] Add durable, delayed, deduplicated message-email processing and recipient-selection tests.
