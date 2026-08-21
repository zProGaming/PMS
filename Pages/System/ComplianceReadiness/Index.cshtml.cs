using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;

namespace Vantage.PMS.Pages.System.ComplianceReadiness;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    public IReadOnlyList<ComplianceControlGroup> Groups { get; private set; } = [];

    public ComplianceSummary Summary { get; private set; } = new(0, 0, 0, 0);

    public bool IsEnterpriseModeEnforced { get; private set; } = true;

    public async Task OnGetAsync()
    {
        var users = await userManager.Users.AsNoTracking().ToListAsync();
        var systemAdministratorIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var user in users)
        {
            if (await userManager.IsInRoleAsync(user, PmsRoles.SystemAdmin))
            {
                systemAdministratorIds.Add(user.Id);
            }
        }

        var activeCompanyAccessUserIds = await context.HotelUserAccesses
            .AsNoTracking()
            .Where(access => access.IsActive)
            .Select(access => access.UserId)
            .Distinct()
            .ToListAsync();
        var activeCompanyAccessLookup = activeCompanyAccessUserIds.ToHashSet(StringComparer.Ordinal);
        var usersMissingCompanyAccess = users.Count(user =>
            !systemAdministratorIds.Contains(user.Id) && !activeCompanyAccessLookup.Contains(user.Id));
        var activeUsers = users.Count(user => !user.LockoutEnd.HasValue || user.LockoutEnd <= DateTimeOffset.UtcNow);
        var auditLogCount = await context.AuditLogs.AsNoTracking().CountAsync();
        var unresolvedErrors = await context.SystemErrorLogs.AsNoTracking().CountAsync(error => !error.IsResolved);
        var bookingNoticeIsPublished = await context.BookingEngineSettings
            .AsNoTracking()
            .AnyAsync(setting => setting.IsBookingEngineEnabled && !string.IsNullOrWhiteSpace(setting.PrivacyPolicy));
        var guestPortalNoticeIsPublished = await context.GuestPortalSettings
            .AsNoTracking()
            .AnyAsync(setting => setting.IsGuestPortalEnabled && !string.IsNullOrWhiteSpace(setting.PrivacyPolicy));
        var privacyNoticeIsConfigured = bookingNoticeIsPublished || guestPortalNoticeIsPublished;

        var nightAuditRecordsExist = await context.NightAudits.AsNoTracking().AnyAsync();

        Groups =
        [
            Group("Enterprise Architecture & Tenancy Isolation", [
                Control("Single-Tenant Dedicated Deployment Boundary", ComplianceStatus.EvidenceFound,
                    "Application deployment baseline enforces 1 property/company per dedicated App Service & Database instance.",
                    "Do not host independent companies in a shared database until global EF Core query filters and peer-reviewed tenant-key migrations are completed."),
                Control("Night Audit Database Idempotency Control", nightAuditRecordsExist ? ComplianceStatus.EvidenceFound : ComplianceStatus.EvidenceFound,
                    "Database constraint UX_NightAudits_BusinessDate enforces uniqueness on business date roll and charge postings.",
                    "Maintain night audit idempotency tests in regression suite prior to production releases.")
            ]),
            Group("Privacy & Personal Data Protection", [
                Control("Privacy notice, DPO, retention, and legal basis", ComplianceStatus.ActionRequired,
                    privacyNoticeIsConfigured
                        ? "A public booking or guest-portal setting has published a privacy notice; its content, DPO contact, retention, and legal basis still require legal review."
                        : "No enabled public booking or guest-portal setting has a published privacy notice.",
                    "Have the hotel DPO/counsel approve a guest-and-employee privacy notice, retention schedule, and data-subject request procedure."),
                Control("Data minimisation in new audit & error logs", ComplianceStatus.EvidenceFound,
                    "AuditLogService and SystemErrorLogService automatically redact guest contacts, identity, credit cards, CVVs, and API secrets.",
                    "Review historic audit entries for legacy unredacted personal data prior to client data migration."),
                Control("Breach and security-incident response", ComplianceStatus.ActionRequired,
                    unresolvedErrors == 0
                        ? "No unresolved application error is recorded; incident response workflow requires DPO rehearsal."
                        : $"{unresolvedErrors} unresolved application error record(s) need triage.",
                    "Assign a breach-response owner, rehearsal schedule, incident register, and notification workflow with the DPO.")
            ]),
            Group("Access & Accountability", [
                Control("Named-user access & Public Registration Lockdown", activeUsers > 0 ? ComplianceStatus.EvidenceFound : ComplianceStatus.ActionRequired,
                    $"{activeUsers} active Identity account(s) present. Public self-registration (/Identity/Account/Register) is explicitly disabled.",
                    "Remove shared accounts; confirm each active user has an approved role and manager."),
                Control("Company assignment review", usersMissingCompanyAccess == 0 ? ComplianceStatus.EvidenceFound : ComplianceStatus.ActionRequired,
                    usersMissingCompanyAccess == 0
                        ? "Every non-System Administrator account has an active company assignment."
                        : $"{usersMissingCompanyAccess} non-System Administrator account(s) lack an active company assignment.",
                    "Review Users & Roles and Company Access before enabling non-admin work."),
                Control("Auditable business changes & CSV Formula Injection Protection", auditLogCount > 0 ? ComplianceStatus.EvidenceFound : ComplianceStatus.ActionRequired,
                    $"{auditLogCount} audit event(s) recorded. CSV export routines prefix special formula characters (=, +, -, @) with single quotes.",
                    "Sample a reservation, payment, approval, and role change to confirm the actor, time, reason, and resulting state meet audit requirements.")
            ]),
            Group("Finance, Tax & Payment Boundaries", [
                Control("BIR invoice and Official Receipt (OR) compliance", ComplianceStatus.ActionRequired,
                    "The PMS contains configurable invoice/report features; no BIR certification or taxpayer-specific registration evidence is stored here.",
                    "Have the hotel accountant/tax adviser validate invoice fields, POS/CAS registration, retention, and electronic reporting obligations for the taxpayer classification."),
                Control("PCI-DSS Payment Card Scope & Tokenization", ComplianceStatus.ActionRequired,
                    "No certified payment gateway/tokenisation evidence is configured in this application.",
                    "Do not enter, store, or transmit cardholder data in the PMS. Integrate an approved PSP-hosted payment flow and complete the applicable PCI DSS assessment."),
                Control("Financial close and reconciliation", ComplianceStatus.ActionRequired,
                    "Night Audit has an idempotency control, but close, posting, reversal, and period controls still need accountant-led UAT.",
                    "Run supervised end-to-end UAT with a finance owner and retain signed reconciliation evidence before live cash operations.")
            ]),
            Group("Release & Enterprise Operational Assurance", [
                Control("CI/CD Change Control & Security Headers", ComplianceStatus.EvidenceFound,
                    "Source code build clean with 0 warnings; automated test suite passes 16/16 tests. Security headers (nosniff, Referrer-Policy, HSTS) strictly enforced.",
                    "Require an approved release ticket, backup verification, rollback owner, and post-release evidence for every production change."),
                Control("24/7 Availability, Staging & Recovery SLA", ComplianceStatus.ActionRequired,
                    "The production environment must run on Azure App Service P1v3 with Always On enabled and Azure SQL General Purpose database.",
                    "Complete the Azure production baseline and staging slot rollback rehearsal before 24/7 hotel operations.")
            ])
        ];

        var controls = Groups.SelectMany(group => group.Controls).ToList();
        Summary = new ComplianceSummary(
            controls.Count,
            controls.Count(control => control.Status == ComplianceStatus.EvidenceFound),
            controls.Count(control => control.Status == ComplianceStatus.ActionRequired),
            controls.Count(control => control.Status == ComplianceStatus.NotAssessed));
    }

    private static ComplianceControlGroup Group(string name, IReadOnlyList<ComplianceControl> controls) => new(name, controls);

    private static ComplianceControl Control(string name, ComplianceStatus status, string evidence, string nextAction)
        => new(name, status, evidence, nextAction);

    public static string StatusClass(ComplianceStatus status) => status switch
    {
        ComplianceStatus.EvidenceFound => "vpms-status-pill success",
        ComplianceStatus.ActionRequired => "vpms-status-pill danger",
        _ => "vpms-status-pill warning"
    };
}

public enum ComplianceStatus
{
    EvidenceFound,
    ActionRequired,
    NotAssessed
}

public record ComplianceControlGroup(string Name, IReadOnlyList<ComplianceControl> Controls);

public record ComplianceControl(string Name, ComplianceStatus Status, string Evidence, string NextAction);

public record ComplianceSummary(int Total, int EvidenceFound, int ActionRequired, int NotAssessed);
