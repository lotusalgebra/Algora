using Algora.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers
{
    public class ProductsController : Controller
    {

        // Mock data for now; replace with ShopifyService or EF later
        private static List<ProductViewModel> _products = new()
    {
        new ProductViewModel { Id = 1, Title = "Sky Blue Silk Saree", Price = 1299, Stock = 10, Tags = "Silk, Blue" },
        new ProductViewModel { Id = 2, Title = "Red Banarasi Cotton Saree", Price = 1599, Stock = 8, Tags = "Banarasi, Cotton, Red" },
    };

        [HttpGet("/products")]
        public IActionResult Index(string search = "")
        {
            var results = string.IsNullOrWhiteSpace(search)
                ? _products
                : _products.Where(p => p.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                                    || p.Tags.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            ViewBag.Search = search;
            return View(results);
        }

        [HttpGet("/products/create")]
        public IActionResult Create() => View(new ProductViewModel());

        [HttpPost("/products/create")]
        public IActionResult Create(ProductViewModel model)
        {
            model.Id = _products.Any() ? _products.Max(p => p.Id) + 1 : 1;
            _products.Add(model);
            return RedirectToAction("Index");
        }

        [HttpGet("/products/edit/{id}")]
        public IActionResult Edit(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost("/products/edit/{id}")]
        public IActionResult Edit(int id, ProductViewModel model)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();
            product.Title = model.Title;
            product.Description = model.Description;
            product.Price = model.Price;
            product.Stock = model.Stock;
            product.Tags = model.Tags;
            return RedirectToAction("Index");
        }

        [HttpGet("/products/details/{id}")]
        public IActionResult Details(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpGet("/products/delete/{id}")]
        public IActionResult Delete(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost("/products/delete/{id}")]
        public IActionResult ConfirmDelete(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product != null) _products.Remove(product);
            return RedirectToAction("Index");
        }
    }
}
