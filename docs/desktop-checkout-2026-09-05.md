# Desktop operations and checkout safeguards

## Scope

This release implements the agreed checkout, daily-work and desktop-consistency package. It does not certify company-wide readiness or legal compliance, alter hosting tiers, or change the production database schema.

## Staff-facing changes

- The home page is now **Daily work**. Front Desk sees arrivals and departures; Cashier sees their open shifts and collection/credit follow-up; Housekeeping sees turnover and room-readiness queues; managers see void, refund and discount decisions.
- Only workspaces permitted by the user's existing roles are available. Selecting another workspace in the URL cannot grant access. Each queue fetches at most eight items and displays the full matching count. Non-empty queues appear first.
- The previous management dashboard remains at `/Overview`, restricted to executive-management roles. Other department roles retain their existing sidebar modules.
- Neutral desktop surfaces, consistent form/action spacing, readable table headers, separate numeric alignment, visible keyboard focus and text-labelled actions are shared across operational screens. Page/reveal fades are removed from the staff shell.
- Checkout uses one shared review in the full page and native dialog, showing every folio, separate debt and credit totals, and the next action for each ledger.

## Settlement rules

1. A stay must be checked in, have an assigned room and have a folio. Closed folios with nonzero balances require Finance correction.
2. Debt on one folio cannot be offset by a credit on another. Voided folios are excluded; transferred folios retain their Accounts Receivable workflow.
3. Unpaid checkout requires an existing authorized manager role and a trimmed 10–500-character reason/collection plan. Approval is never inferred from a previous check-in override.
4. Guest credits require explicit referral acknowledgement. Checkout issues no refund; credit-bearing folios stay open. Only open, exactly zero-balance folios close.
5. A changed stay, charge or payment invalidates the previous review. The response refreshes the ledger and requires another confirmation.
6. Checkout, room-dirty status, turnover-task creation and the decision audit commit in one SQL transaction. Repeated/concurrent successful submissions return the existing outcome without creating another checkout decision or turnover task.
7. Direct folio payments and direct/routed Front Office charges coordinate through the same transaction-owned reservation lock and recheck ledger status. Payment duplicate/balance checks run inside that lock. Checkout uses serializable isolation for its complete ledger review.
8. The decision audit records actor, UTC event time, reason, debt, credit, relevant folio IDs and credit acknowledgement. Normal entity audits are also retained. This is transactional audit recording, not an immutable external audit archive.

## Verification

Run on Windows with .NET 10, SQL Server LocalDB (`MSSQLLocalDB`) and Microsoft Edge:

```powershell
dotnet test tests/Vantage.PMS.Tests/Vantage.PMS.Tests.csproj --configuration Release
```

The suite creates a uniquely named local `VantageCheckoutTests_*` database and deletes only that database on teardown. It does not connect to production. Test authentication exists only in the excluded test assembly. The test-only loopback HTTP host allows its antiforgery cookie on HTTP; production retains secure-only cookies and all tests keep antiforgery validation enabled.

Coverage includes multiple folios, debt/credit separation, override roles and reason validation, stale reviews, missing/closed ledgers, concurrent checkout, concurrent payment, rollback after a database write, role-restricted routes, forged approvals with valid antiforgery tokens, missing antiforgery rejection, and real browser checkout submission.

Desktop browser checks cover 1366×768 and 1920×1080, plus a 1093×614 CSS viewport at 1.25 device scale to approximate 125% scaling on a 1366×768 display. Checks include populated and empty role queues, long names, amount columns, disabled/enabled decision controls, script errors, page overflow and clipped primary labels. Screenshots are saved to `artifacts/desktop-qa` and uploaded by CI. This is targeted desktop QA, not a complete accessibility certification or every-module visual regression suite.

## Deployment and remaining operational gates

The existing master-branch GitHub Actions workflow builds, tests and publishes before deployment using Azure OpenID Connect. No SQL firewall rule or migration is required for this release. Validate authenticated department workflows with named staff accounts after deployment.

Existing role permissions are retained: Front Desk posts guest-folio payments; Cashier can review the filtered receipts and use its existing Finance workflows. Expanding cashier settlement access needs a separately reviewed permission design.

Company rollout still needs named-user UAT and sign-off, backup/restore and recovery drills, performance/load targets and suitable hosting capacity, approved privacy/retention procedures, and a wider transactional review of ledger writers outside direct Front Office payment/charge posting (for example refunds, group transfers and batch/night-audit posting). No claim is made that this release alone completes those gates.
