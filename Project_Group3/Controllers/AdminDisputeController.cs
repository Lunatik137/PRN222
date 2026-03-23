using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Project_Group3.Models;
using Project_Group3.Repository.Interfaces;

namespace Project_Group3.Controllers
{
    public class AdminDisputeController : Controller
    {
        private readonly IDisputeRepository _repo;


        public AdminDisputeController(IDisputeRepository repo)
        {
            _repo = repo;

        }

        public IActionResult IndexDispute()
        {
            var data = _repo.GetAll();
            return View("IndexDispute",data);
        }

        public IActionResult Details(int id)
        {
            var d = _repo.GetById(id);
            return View(d);
        }
        public IActionResult Edit(int id)
        {
            var d = _repo.GetById(id);
            return View(d);
        }

        [HttpPost]
        public async Task<IActionResult> Update(int id, string status, string resolution)
        {
            var dispute = _repo.GetById(id);
            if (dispute.status == "RESOLVED")
            {
                return RedirectToAction("Details", new { id });
            }
            _repo.Update(id, status, resolution);

            return RedirectToAction("Details", new { id });
        }
    }
}
