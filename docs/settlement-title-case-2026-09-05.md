# Desktop Labels And Settlement Controls — September 5, 2026

## Scope

This release continues the desktop-first operations work. It standardizes interface labels to Title Case, such as **Housekeeping Task**, without replacing the font family or changing guest names, business data, entered notes, binding values, or explanatory sentences.

- Static headings, navigation, actions, field labels, captions, short placeholders, and dialog titles were reviewed mechanically across the Razor views. Acronyms and HTML entities are preserved.
- Generated property labels now separate PascalCase words, and enum choices display readable labels such as **Credit Card** and **Out Of Order** while keeping their original submitted values.
- `scripts/title-case-ui.cjs --check` runs safety assertions and detects static label regressions in CI. Dynamic user data is intentionally excluded.
- Cashier settlement has a dedicated, searchable, paginated Finance workspace and reuses the controlled payment form. Cashiers do not gain reservation-management access.
- Payment summaries use three distinct, wrapping columns. Desktop screenshots cover 1366×768, 1920×1080, and a 1093×614 CSS viewport at 1.25 scale (an approximation of desktop 125% scaling, not native OS zoom testing).

## Financial Controls

- Refunds require a completed source receipt, a matching folio, a positive two-decimal amount within the remaining refundable amount, and a documented reason.
- Requester identity, timestamps, request numbers, approvals, and processing fields are server-owned. Posted navigation objects and forged identities are not saved.
- A different finance manager must approve the request. The approver cannot also process it. The requester may process after independent approval, allowing a two-person workflow.
- Refund processing requires the operator's open cashier shift. Cash payouts cannot exceed expected cash in that shift. Processing records the payout; it does not initiate a payment-gateway transfer.
- Reservation-ledger and request locks serialize processing with checkout, other refunds, and payment posting. Processing retries do not create a second reversal.
- The original receipt remains Completed, and a negative completed receipt offsets it once. Its cashier trace points to that negative receipt. A nonzero balance created on a closed folio reopens that folio for follow-up.
- Void requests now have a terminal Processed state. This workflow supports unlocked folio charges and original payment receipts on open folios. Payment voids update the matching cashier trace and cannot alter closed/audited shifts or receipts with processed refunds. POS and issued-document voiding are deliberately excluded from this generic workflow.
- Payment posting, refunds, cash drops, and shift closure use a shared shift lock. Cashiers cannot close another operator's shift; a finance manager can manage the shift. Negative cash counts and excessive cash drops are rejected.
- Decisions and their actors, reasons, source references, before/after states, and UTC occurrence times are recorded with the transaction.

## Housekeeping Handoff

- Completion is idempotent and cancelled tasks cannot be completed.
- Completing the final task for a vacant dirty room makes it Clean, not Available. Occupied rooms are not released by task completion.
- Open work blocks readiness advancement. Inspection and release require a housekeeping supervisor, General Manager, or System Administrator.
- Status updates reject stale submissions and checked-in occupancy conflicts.
- Out-of-order recovery proceeds through Maintenance → Dirty → Clean → Inspected → Available, with reasons for maintenance changes.
- New tasks on vacant ready rooms remove those rooms from ready inventory. Guest-request task creation is idempotent and uses the same room lock.
- The room board distinguishes Available inventory from Clean/Inspected rooms awaiting review or release. Inaccessible front-office links are hidden from the affected cashier/housekeeping navigation.

## Verification

- Release build and 93 automated tests passed locally, including real SQL Server LocalDB transactions, concurrency tests, authorization, antiforgery, real cashier/checkout browser submissions, capitalization/data-preservation checks, and desktop overflow checks.
- Browser QA uses synthetic actors and generated training records in an isolated, disposable LocalDB database. No production guest records or production financial entries were changed for these tests.
- Screenshots are generated under `artifacts/desktop-qa` and uploaded by the GitHub Actions release workflow.
- No database schema migration is required. Processed was appended to the existing integer approval enum without changing prior numeric values.

## Operations And Remaining Boundaries

- Finance must inspect **Payment Integrity** for legacy receipts already marked Refunded. Those originals are excluded from existing folio totals and may have a separate negative entry, causing a double reversal. This release flags them; it does not silently repair historical balances or accounting postings.
- Previously approved requests lacking a valid source or independent approval will be blocked. Review them and recreate a valid request where appropriate.
- Configure at least two appropriate operational accounts for independent financial approval. Do not use shared administrator credentials as the daily operating model.
- This is a scoped release, not a certification that every module is company-ready or legally compliant. Dedicated company isolation, restore drills, load testing, tax/privacy sign-off, accounting reconciliation, and operator UAT remain separate deployment gates.
- Paid hosting settings, database firewall rules, and production data were not changed by this release. Deployment should use the normal master-branch CI pipeline, followed by live health and static-asset verification.
- Rollback is a redeployment of a reviewed prior application build; it must not reverse posted financial transactions or remove audit evidence. Coordinate any rollback after a processed adjustment with Finance.
