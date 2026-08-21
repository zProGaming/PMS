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

        Groups =
        [
            Group("Privacy & Personal Data", [
                Control("Privacy notice, DPO, retention, and legal basis", ComplianceStatus.ActionRequired,
                    privacyNoticeIsConfigured
                        ? "A public booking or guest-portal setting has published a privacy notice; its content, DPO contact, retention, and legal basis still require legal review."
                        : "No enabled public booking or guest-portal setting has a published privacy notice.",
                    "Have the hotel DPO/counsel approve a guest-and-employee privacy notice, retention schedule, and data-subject request procedure."),
                Control("Data minimisation in new audit entries", ComplianceStatus.EvidenceFound,
                    "New audit values redact common guest, identity, payment, and secret fields.",
                    "Review existing audit and error-log records for historic personal data before client use."),
                Control("Breach and security-incident response", ComplianceStatus.ActionRequired,
                    unresolvedErrors == 0
                        ? "No unresolved application error is recorded; this is not evidence of breach readiness."
                        : $"{unresolvedErrors} unresolved application error record(s) need triage.",
                    "Assign a breach-response owner, rehearsal schedule, incident register, and notification workflow with the DPO.")
            ]),
            Group("Access & Accountability", [
                Control("Named-user access", activeUsers > 0 ? ComplianceStatus.EvidenceFound : ComplianceStatus.ActionRequired,
                    $"{activeUsers} active Identity account(s) are present.",
                    "Remove shared accounts; confirm each active user has an approved role and manager."),
                Control("Company assignment review", usersMissingCompanyAccess == 0 ? ComplianceStatus.EvidenceFound : ComplianceStatus.ActionRequired,
                    usersMissingCompanyAccess == 0
                        ? "Every non-System Administrator account has an active company assignment."
                        : $"{usersMissingCompanyAccess} non-System Administrator account(s) lack an active company assignment.",
                    "Review Users & Roles and Company Access before enabling non-admin work."),
                Control("Auditable business changes", auditLogCount > 0 ? ComplianceStatus.EvidenceFound : ComplianceStatus.ActionRequired,
                    $"{auditLogCount} audit event(s) are recorded.",
                    "Sample a reservation, payment, approval, and role change to confirm the actor, time, reason, and resulting state are sufficient for the hotel's audit policy.")
            ]),
            Group("Finance, Tax & Payment Boundaries", [
                Control("BIR invoice and records validation", ComplianceStatus.ActionRequired,
                    "The PMS contains configurable invoice/report features; no BIR certification or taxpayer-specific registration evidence is stored here.",
                    "Have the hotel accountant/tax adviser validate invoice fields, POS/CAS registration, retention, and electronic reporting obligations for the taxpayer classification."),
                Control("Payment-card scope", ComplianceStatus.ActionRequired,
                    "No certified payment gateway/tokenisation evidence is configured in this application.",
                    "Do not enter, store, or transmit cardholder data in the PMS. Integrate an approved PSP-hosted payment flow and complete the applicable PCI DSS assessment."),
                Control("Financial close and reconciliation", ComplianceStatus.ActionRequired,
                    "Night Audit has an idempotency control, but close, posting, reversal, and period controls still need accountant-led UAT.",
                    "Run supervised end-to-end UAT with a finance owner and retain signed reconciliation evidence before live cash operations.")
            ]),
            Group("Release & Operational Assurance", [
                Control("Change control", ComplianceStatus.EvidenceFound,
                    "Source is versioned and CI validates build/test execution.",
                    "Require an approved release ticket, backup verification, rollback owner, and post-release evidence for every production change."),
                Control("Availability and recovery", ComplianceStatus.ActionRequired,
                    "The current hosting configuration does not yet meet the runbook's always-on, staging, monitoring, and recovery-rehearsal baseline.",
                    "Complete the Azure production baseline before 24/7 hotel operations or contractual service commitments.")
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
