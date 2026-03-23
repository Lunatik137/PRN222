using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Models;

namespace PRN222_Group3.Controllers
{
    public class ProductController : Controller
    {
        private readonly CloneEbayDbContext _context;


        public ProductController(CloneEbayDbContext context)
        {
            _context = new CloneEbayDbContext();
        }

        public async Task<bool> deleteProduct(int productID)
        {
            var product = await _context.Products.FindAsync(productID);
            if (product != null)
            {
                _context.Products.Remove(product);
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products.ToListAsync();
        }
        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            // Chỉ cho phép 2 trạng thái
            if (status != "pending" && status != "delete")
                return BadRequest("Invalid status");

            product.Status = status;
            await _context.SaveChangesAsync();

            return RedirectToAction("Product");
        }


        public async Task<IActionResult> Product(string search)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Title.Contains(search));
            }

            var products = await query.ToListAsync();
            ViewBag.Search = search;
            return View(products);
        }

    }
}
