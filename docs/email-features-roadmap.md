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
| `EmailOnNewMessage` preferences | Deferred — do not build yet |
| Message email debounce | 5 minutes |
| Background worker | EF outbox table + `BackgroundService` (no Hangfire) |
| Scope for now | `#170` foundation + `#90` password reset; `#171` later |

## Assessment

The parent issue is a reasonable dependency grouping: build email delivery first, then password reset, then message
notifications. The main adjustment is to treat message-email notifications as a larger feature and split it into read
state, preferences, and queued/debounced delivery rather than sending an email inline for every message.

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

- Add per-user notification preferences, at minimum `EmailOnNewMessage`, exposed through account settings.
- Add a per-member chat read state such as `LastReadMessageId` or `LastReadAt`, plus an authenticated mark-read endpoint
  called when a conversation is viewed. This is required because [
  `Backend/BandFounder/BandFounder.Application/Services/MessageService.cs`](../Backend/BandFounder/BandFounder.Application/Services/MessageService.cs)
  currently knows only that a message was saved, not whether recipients have read it.
- Fix the incorrect `DbSet<Account> Messages` declaration in [
  `Backend/BandFounder/BandFounder.Infrastructure/BandFounderDbContext.cs`](../Backend/BandFounder/BandFounder.Infrastructure/BandFounderDbContext.cs)
  while introducing the schema changes.

## 4. Deliver useful, non-spammy message notifications

- After a message is successfully persisted in `MessageService.SendMessage`, publish/enqueue one notification candidate
  per recipient except the sender; do not send SMTP inline in the HTTP request.
- Process candidates in a background worker with durable storage/outbox semantics. Delay and deduplicate by
  recipient/chatroom (for example 5–10 minutes), then send only if the conversation is still unread and the recipient
  has email notifications enabled.
- Send one summary email with sender/chat name, a safe text snippet, and a configurable deep link to
  `/messages/{chatRoomId}`. Escape user-generated content and avoid placing full sensitive messages in email by default.
- Test recipient selection, sender exclusion, preferences, read-before-delay suppression, grouping, retries, and
  idempotency. Treat authenticated WebSocket presence as a later optimization; the current WebSocket path runs before
  authentication and tracks chatrooms rather than users.

## Definition of done

- Email secrets are externalized and no real credentials are committed.
- Password reset is enumeration-resistant, rate-limited, expiring, single-use, and tested end to end.
- Message emails are optional, delayed/grouped, based on durable unread state, and cannot block message posting.
- Provider failures are observable and retryable without duplicate emails.

## Implementation todos

- [ ] Implement provider-independent email delivery, externalized configuration, templates, tests, and migration
  readiness.
- [ ] Add secure opaque reset tokens, backend endpoints, frontend flows, rate limits, and end-to-end tests.
- [ ] Add email preferences and durable per-chat read state with account and messaging APIs.
- [ ] Add durable, delayed, deduplicated message-email processing and recipient-selection tests.
