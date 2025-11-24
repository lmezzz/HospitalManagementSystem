using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebManagementSystem;

public class RoleController : Controller
{
    private readonly HmsContext _context;

    public RoleController(HmsContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Roles.ToListAsync());
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Role role)
    {
        if (ModelState.IsValid)
        {
            _context.Add(role);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(role);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null) return NotFound();
        return View(role);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Role role)
    {
        if (id != role.RoleId) return BadRequest();
        if (ModelState.IsValid)
        {
            _context.Update(role);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(role);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null) return NotFound();
        return View(role);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var role = await _context.Roles.FindAsync(id);
        _context.Roles.Remove(role!);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
