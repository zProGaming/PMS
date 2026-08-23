# Enterprise Company Readiness Audit

**Reviewed:** 23 August 2026
**Scope:** source code, application controls, automated regression tests, and the currently known Azure operating baseline.

## Deployment decision

**Not approved for shared-database, multi-company SaaS or unattended 24/7 hotel operations.**

The currently supported boundary is **one company/property per dedicated application and database**. Company-code login and user-access records help administration, but they do not apply a global tenant identifier and query filter to every business record. A shared database must remain out of scope until tenant scoping, tests, backup/export controls, and an independent review are complete.

This is a technical readiness assessment. It is not a legal, tax, payroll, security, accessibility, or PCI certification.

## Evidence found in the application

| Area | Current evidence | Boundary |
| --- | --- | --- |
| Access | Staff self-registration is blocked; Identity lockout and role policies are configured. | Each account still needs named-user, role, manager, and company-assignment review. |
| Audit and errors | New audit values redact common personal, payment, statutory-ID, and secret fields. Error logs redact common connection/query secrets. | Historic log records need controlled retention and privacy review. |
| Night audit | Database uniqueness protects a completed business date and automated charge idempotency keys. | Accountant-led UAT is still required for closing, posting, corrections, reversals, and reconciliation. |
| Exports | CSV formula-like leading values are neutralised. | Report accuracy, distribution, retention, and management sign-off remain operational controls. |
| Privacy notice | Public booking and guest-portal data-entry points link to the hotel-configured public notice. | DPO/counsel must approve the notice, legal basis, retention schedule, DSAR procedure, and incident plan. |
| Desktop UI | Sidebar group icons have text labels and common SVG treatment; the control register has status, evidence, and next-action columns. | Front-desk, finance, and approval workflows still need role-specific task queues and desktop UAT at the target workstation resolution. |
| Regression suite | Build and test execution cover audit/error redaction, CSV injection protection, Night Audit model uniqueness, and role/redaction checks. | Tests are not a substitute for end-to-end property, finance, recovery, or security testing. |

## Launch-stop register

| Priority | Finding | Required closure evidence |
| --- | --- | --- |
| P0 | No global tenant isolation for independent companies sharing one database. | Dedicated app/database per customer, or a completed tenant-key migration with cross-company regression tests. |
| P0 | The present free App Service and auto-pausing Azure SQL configuration can cause cold starts and lacks a production availability baseline. | Cost-approved always-on hosting, staging/rollback, monitoring, point-in-time restore rehearsal, and 24/7 escalation owner. |
| P0 | No certified PSP-hosted tokenisation integration or applicable PCI validation is evidenced. | Approved payment-provider design and confirmation that the PMS does not handle raw cardholder data. |
| P0 | BIR/CAS/POS/e-invoice obligations are taxpayer-specific and not certified by this application. | Accountant/tax-adviser validation, registration/permit evidence, invoice samples, and reconciled records. |
| P0 | Privacy governance is not configured by code alone. | Approved notice, DPO contact, data map, retention schedule, DSAR procedure, and breach rehearsal. |

## Process and desktop UI priorities

1. Give each role a desktop work queue: arrivals, departures, room exceptions, unsettled folios, approvals, and night-audit readiness.
2. For irreversible actions—checkout, void, refund, approval, posting, and close—show the actor, reason, timestamp, confirmation, and printable audit reference.
3. Use short, textual status labels beside colour; keep critical actions labelled and avoid icon-only controls.
4. Replace dense finance/report card walls with a table or queue showing owner, due date, monetary impact, status, and a single next action.
5. Run supervised, end-to-end UAT for reservation-to-payment-to-close, inventory purchasing, payroll/labour, report reconciliation, and recovery.

## Required release gates

1. Verified, restorable backup and a non-production migration/rollback rehearsal.
2. Staging smoke test using non-production data, including permission boundaries and one reservation-to-finance workflow.
3. A named owner and retained closure evidence for every P0 item.
4. Approved change record, CI result, release version, rollback owner, post-release smoke test, and monitoring record.
5. DPO/counsel, tax adviser/accountant, and payment provider approval within their respective scopes.

For the detailed product-level assessment and runbook, see [Company Readiness Audit](COMPANY_READINESS_AUDIT_2026-08-21.md) and [Production Release Runbook](PRODUCTION_RELEASE_RUNBOOK.md).
