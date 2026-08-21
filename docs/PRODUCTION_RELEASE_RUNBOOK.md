# Vantage PMS production release runbook

## Supported deployment boundary

Vantage PMS is currently supported as **one company per application and database**. The company-code login and user-access table are useful account-assignment controls, but the business schema is not yet globally tenant-scoped. Do not host independent companies in the same database until the dedicated multi-tenant migration is completed, peer-reviewed, and tested for every module.

Each customer deployment must therefore have its own Azure App Service, Azure SQL database, Key Vault, backup policy, and production URL.

## Release gates

Before a production release, the release owner must complete all of the following:

1. Take and verify a restorable, transactionally consistent database backup.
2. Restore that backup to a non-production database and rehearse the upgrade and rollback.
   Before applying `AddNightAuditIdempotency`, verify that `NightAudits` does not contain duplicate `BusinessDate` values; resolve any duplicates under finance approval before applying the unique index.
3. Require a protected production configuration:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `Startup__RunIdentitySeed=false`
   - `Startup__RequireIdentitySeed=false`
   - connection strings and bootstrap credentials stored outside source control.
4. Provision staff accounts through **Admin > Users & Roles**, then grant the company assignment through **Admin > Company Access**. Public self-registration is intentionally disabled.
5. Verify that the emergency administrator can sign in using the intended company code and that a non-admin role is limited to its approved modules.
6. Run the CI build and test suite and preserve the successful run URL with the release record.
7. Complete the smoke tests below against staging before touching production.

## Azure operating baseline

The current free App Service and auto-pausing SQL configuration are not appropriate for a live hotel operation. Obtain cost approval and use a production plan that supports the following operational controls:

- Always-on application hosting, 64-bit worker process, HTTPS-only access, and a health probe at `/health/live`.
- A staging slot or equivalent blue/green environment before production swap.
- Azure SQL configured to avoid operational cold starts during property hours, with automated point-in-time restore and retained backups.
- Managed identity and Key Vault references for secrets; rotate any credential that was previously shared through chat, shell history, or local configuration.
- Restrictive network access between the app and database, with no broad temporary firewall rules left in place.
- Centralised telemetry, alerting, and an owner for 24/7 incident escalation.

## Deployment sequence

1. Announce the maintenance window and freeze configuration changes.
2. Confirm the backup from the release gates and capture the currently deployed artifact version.
3. Deploy the release artifact to staging. Do not run demo seeding in staging or production.
4. Apply reviewed EF Core migrations through the release pipeline only, after the backup is confirmed. The web application must not create schema or seed data on startup.
5. Run the smoke tests in staging, including one full reservation-to-payment scenario using non-production data.
6. Swap or deploy to production during the approved window.
7. Run the production smoke tests. Monitor HTTP failures, sign-in failures, database resource use, and response time for at least one business cycle.
8. If a critical test fails, stop the rollout, restore the prior app artifact, and use the verified database recovery plan when schema/data rollback is required.

## Mandatory smoke tests

- `GET /health/live` returns `200` without requiring a database connection.
- Unauthenticated access to `/Identity/Account/Register` returns `404`.
- Login succeeds for a confirmed active staff user and rejects an invalid password without revealing which field failed.
- Five invalid password attempts lock the test account; a System Administrator can reactivate it in Users & Roles.
- A Front Desk role can create, modify, check in, and check out a reservation according to its permissions.
- A cashier posts one payment, prints/exports its receipt, and the related finance entry appears exactly once.
- Night Audit is run only in staging during rehearsal and is reviewed for correct lock/post behaviour before a production use.
- CSV exports open without interpreting data that starts with `=`, `+`, `-`, or `@` as a formula.
- Core desktop routes render at the target workstation resolution without horizontal clipping, overlay collisions, or unlabeled icon-only controls.

## Current release blockers for multi-company SaaS

Do not mark a shared-database, multi-company rollout as ready until all domain records and queries enforce the active company/tenant identifier, role assignments are scoped to that identifier, cross-company regression tests pass, and tenant-aware backup/export/audit controls are independently reviewed.
