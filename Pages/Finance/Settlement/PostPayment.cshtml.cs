using Vantage.PMS.Data;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Finance.Settlement;

// A narrow finance endpoint reuses the posting controls without granting Front Office access.
public class PostPaymentModel(ApplicationDbContext context, FinanceService finance)
    : FrontOffice.Folios.PostPaymentModel(context, finance)
{
    public override bool IsSettlementWorkspace => true;
}
