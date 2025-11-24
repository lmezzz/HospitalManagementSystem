using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebManagementSystem;

public class AppUserController : Controller
{
    private readonly HmsContext _context;

    public AppUserController(HmsContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var users = _context.AppUsers.Include(u => u.Role);
        return View(await users.ToListAsync());
    }

    public IActionResult Create()
    {
        ViewData["Roles"] = _context.Roles.ToList();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(AppUser user)
    {
        if (ModelState.IsValid)
        {
            _context.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewData["Roles"] = _context.Roles.ToList();
        return View(user);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null) return NotFound();
        ViewData["Roles"] = _context.Roles.ToList();
        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, AppUser user)
    {
        if (id != user.UserId) return BadRequest();
        if (ModelState.IsValid)
        {
            _context.Update(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewData["Roles"] = _context.Roles.ToList();
        return View(user);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.AppUsers.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        _context.AppUsers.Remove(user!);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
