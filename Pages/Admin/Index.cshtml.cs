using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;

namespace Vantage.PMS.Pages.Admin;

public class IndexModel(ApplicationDbContext context, RoleManager<IdentityRole> roleManager) : PageModel
{
    public int Hotels { get; private set; }

    public int Properties { get; private set; }

    public int Departments { get; private set; }

    public int Rooms { get; private set; }

    public int RoomTypes { get; private set; }

    public int Roles { get; private set; }

    public async Task OnGetAsync()
    {
        Hotels = await context.Hotels.AsNoTracking().CountAsync();
        Properties = await context.Properties.AsNoTracking().CountAsync();
        Departments = await context.Departments.AsNoTracking().CountAsync();
        Rooms = await context.Rooms.AsNoTracking().CountAsync();
        RoomTypes = await context.RoomTypes.AsNoTracking().CountAsync();
        Roles = await roleManager.Roles.AsNoTracking().CountAsync();
    }
}
