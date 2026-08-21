# Company readiness audit — 21 August 2026

## Decision

**Do not approve Vantage PMS for multi-company SaaS or unattended 24/7 hotel operations yet.** It can be prepared for a single-property, supervised pilot only after every launch-stop item below has a named owner and recorded closure evidence. This is a product and technical audit, not legal, tax, payroll, security, or PCI certification.

## Scope and evidence reviewed

- Razor Pages application, EF Core model/migrations, authorization policies, validation rules, error handling, audit logging, UI navigation, tests, and release tooling.
- The deployed application and Azure configuration were checked during the release assessment. Current free App Service and auto-pausing Azure SQL configuration cause cold-start risk and do not meet a 24/7 property SLA.
- Build: `dotnet build Vantage.PMS.csproj -c Release --no-restore` — passed with zero warnings/errors.
- Automated checks: `dotnet test tests/Vantage.PMS.Tests/Vantage.PMS.Tests.csproj -c Release --no-restore` — 16 passed.

## Launch-stop findings

| Priority | Area | Finding | Required closure evidence |
| --- | --- | --- | --- |
| P0 | Tenancy | Company access exists, but business records and queries are not globally tenant-scoped. | One property/company per application and database, or a peer-reviewed tenant-key migration with cross-company isolation tests for every module. |
| P0 | Availability | Free App Service and auto-pausing serverless SQL can delay the first operational request; no staging slot, always-on baseline, monitored recovery rehearsal, or 24/7 alert ownership is in place. | Approved production Azure plan, staging/rollback path, point-in-time restore rehearsal, monitoring/alert evidence, named escalation owner. |
| P0 | Privacy | The public privacy route now displays the hotel-configured booking/portal notice, and public forms link to it. The hotel still must approve the content, DPO contact, legal basis, retention schedule, DSAR process, and breach process. | DPO/counsel approval, published notice, staff procedure, retention register, and incident drill record. |
| P0 | Payments | No certified PSP/tokenisation flow or PCI assessment evidence is configured. | Approved PSP-hosted payment flow; confirmation that the PMS never stores/processes/transmits cardholder data; applicable PCI validation. |
| P0 | Finance and tax | Invoice, cashier, POS, and reporting features are not proof of BIR/CAS/POS/e-invoice compliance for a specific taxpayer. | Accountant/tax-adviser UAT, registration/permit evidence, sample invoices, reconciliation and retention sign-off. |

## Module-by-module assessment

| Module | Process assessment | Desktop UI assessment | Current status | Next control |
| --- | --- | --- | --- | --- |
| Identity and access | Registration is disabled, lockout is enabled, and policies restrict modules. Role/company assignment must be reviewed per named user. | Login is compact; managers still need a simple effective-access review in the user screen. | Conditional | Quarterly access review, emergency-admin procedure, remove shared accounts. |
| Front desk and reservations | Reservation lifecycle exists; confirm change, cancellation, no-show, deposit, and handover behaviour in supervised UAT. | Core desktop routes are usable; operators need task-oriented work queues rather than menu hunting. | Conditional | Role-based arrival/departure/exception worklists and UAT scripts. |
| Night audit and finance | Night-audit charges are protected by business-date/charge idempotency indexes. Close, correction, reversal, and posting workflows remain accountant-led. | Finance/report screens are information dense; long card walls reduce scan speed. | Not ready for live cash | Signed day-close reconciliation, reversal approval policy, printed/electronic evidence tests. |
| Cashier, POS, invoices | Functional workflows do not establish tax or payment compliance. | Receipting actions must surface status, audit reference, and next action clearly. | Not ready for live card/tax operations | PSP integration and taxpayer-specific BIR validation. |
| Housekeeping and rooms | Status workflows require shift-handover testing and exception ownership. | Room state should be readable at a glance, with colour backed by plain-text state labels. | Pilot/UAT | Shift checklist and room-status exception queue. |
| Revenue, booking engine, guest portal | Booking request conversion is transactional. Public privacy notice is now rendered from the enabled configuration and linked where guest data is entered. | Public pages give clearer privacy context; the notice must be written and approved by the hotel. | Conditional | Publish reviewed notice; test booking, portal lookup, cancellation, and expiry scenarios. |
| Inventory, purchasing, AP | Approval, receiving, invoice, and period-close workflows need segregation-of-duties testing. | Desktop tables need saved filters, exception counts, and approval context before high-volume use. | UAT required | Three-way-match and approval-matrix UAT with finance/procurement owners. |
| Payroll and labour costing | Sensitive employee data raises privacy, labour, and payroll obligations. | Review screens need clear approval state and exception explanations. | UAT required | HR/payroll owner validation, access review, retention schedule, statutory calculation verification. |
| Reports and exports | CSV formula injection is neutralised. Report accuracy, cutoff, and source-of-truth must be agreed per report. | The report catalogue remains visually dense; prioritise a short list of operational reports and clear date/property context. | Conditional | Report catalogue, reconciliations, export approval and distribution controls. |
| System audit and errors | New audit entries redact common personal, payment, and secret fields; error logs redact connection/query secrets. Historic logs were intentionally not mass-edited and need a controlled review. | The Compliance Control Register gives managers evidence and next actions in one desktop table. | Conditional | Retention policy, protected audit export, periodic log review, incident drill. |

## UI and workflow priorities

1. Make the desktop landing page role-specific: arrivals, departures, unsettled folios, room exceptions, approvals, and night-audit readiness should be the first scan—not a generic menu.
2. Keep sidebar icons recognisable and always pair them with text. The current SVG/icon and plain-language quick-action clean-up should be treated as the baseline; never rely on abbreviations alone.
3. Replace large report/finance card collections with a compact table or queue that shows owner, status, due date, monetary impact, and one primary next action.
4. Use a consistent state vocabulary across reservation, room, folio, approval, and close processes. Colour must reinforce—not replace—the text state.
5. Add confirmation, reason, actor, timestamp, and printable audit reference for irreversible actions: check-out, void/reversal, approval, posting, close, and night audit.
6. Define desktop acceptance at the actual front-desk resolution: no clipping, horizontal table escape, overlapping fixed controls, icon-only critical actions, or ambiguous primary buttons.

## Control changes included in this release candidate

- Public `/Privacy` is anonymous and renders the enabled booking/portal privacy notice without treating it as a generic placeholder.
- Booking and guest-portal data-entry screens link to that notice; booking validation now creates a high-severity finding when public booking is enabled without one.
- New audit entries redact common identity, guest, contact, address, payment, statutory-ID, and secret fields. Error logging redacts connection-string/query secrets.
- System Management has a Compliance Control Register with current evidence, launch-stop wording, and next actions.
- Night Audit cannot create duplicate date/charge postings through its database uniqueness control.
- CI builds the application and test project; the regression suite covers CSV injection protection, audit/error redaction, and Night Audit model uniqueness.

## Required release gates

1. Backup, restore, and migration rehearsal outside production.
2. Staging sign-off with a reservation-to-cashier-to-finance scenario using non-production data.
3. Named owner and evidence for every P0 row above.
4. Staff training and role acceptance for front desk, cashier, night audit, finance, housekeeping, purchasing, HR/payroll, and system administration.
5. DPO/counsel, tax adviser, accountant, and payment acquirer confirmation for their respective boundaries.
6. Change ticket, rollback owner, release version, CI result, smoke-test record, and post-release monitoring record.

## Reference obligations to validate with specialists

- Philippine Data Privacy Act and its implementing rules: <https://privacy.gov.ph/data-privacy-act/> and <https://privacy.gov.ph/implementing-rules-regulations-data-privacy-act-2012/>.
- National Privacy Commission breach reporting procedure: <https://privacy.gov.ph/exercising-breach-reporting-procedures/>.
- BIR invoicing/electronic reporting regulations: <https://bir-cdn.bir.gov.ph/BIR/pdf/RR%207-2024%20%28final%29.pdf> and <https://bir-cdn.bir.gov.ph/BIR/pdf/RR%20No.%2011-2025.pdf>.
- PCI DSS: <https://www.pcisecuritystandards.org/standards/pci-dss/>.
