using Microsoft.AspNetCore.Authorization;
using System;
using Microsoft.AspNetCore.Mvc;
using PRN222_Group3.Models;
using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Service;


namespace PRN222_Group3.Controllers
{
    public class StoresDashboardController : Controller
    {
        private readonly CloneEbayDbContext _db;
        private readonly StoreStatsService _statsService;

        public StoresDashboardController(CloneEbayDbContext db, StoreStatsService statsService)
        {
            _db = new();
            _statsService = statsService;
        }

        // /StoresDashboard/StoresDashboard?filter=all|upgrade|downgrade
        public async Task<IActionResult> StoresDashboard(string filter = "all")
        {
            var stores = await _db.Stores.ToListAsync();
            var list = new List<AdminStoreListItemVm>();

            foreach (var store in stores)
            {
                var stats = await _statsService.GetStatsAsync(store.Id); // trả về 5 thông số

                var recommended = StoreLevelRules.GetRecommendedLevel(stats);

                bool canUpgrade = recommended > (StoreLevel)store.StoreLevel;
                bool canDowngrade = recommended < (StoreLevel)store.StoreLevel;

                list.Add(new AdminStoreListItemVm
                {
                    StoreId = store.Id,
                    StoreName = store.StoreName,
                    CurrentLevel = (StoreLevel)store.StoreLevel,
                    TotalOrders = stats.TotalOrders,
                    TotalSales = stats.TotalSales,
                    ReturnRate = stats.ReturnRate,
                    DisputeRate = stats.DisputeRate,
                    DefectRate = stats.DefectRate,
                    CanUpgrade = canUpgrade,
                    CanDowngrade = canDowngrade,
                    TargetLevel = recommended
                });
            }

            // áp dụng filter
            filter = filter?.ToLower() ?? "all";
            switch (filter)
            {
                case "upgrade":
                    list = list.Where(x => x.CanUpgrade).ToList();
                    break;
                case "downgrade":
                    list = list.Where(x => x.CanDowngrade).ToList();
                    break;
                default:
                    filter = "all";
                    break;
            }

            ViewBag.Filter = filter;

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeLevel(int storeId, StoreLevel targetLevel)
        {
            var store = await _db.Stores.FindAsync(storeId);
            if (store == null) return NotFound();

            store.StoreLevel = (byte)targetLevel; // Explicit cast to byte

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(StoresDashboard));
        }
    }
}






public class StoreLevelCriteria
{
    public int MinTotalOrders { get; init; }
    public decimal MinTotalSales { get; init; }
    public decimal MaxReturnRate { get; init; }
    public decimal MaxDisputeRate { get; init; }
    public decimal MaxDefectRate { get; init; }

    public bool Meets(StoreStats s)
    {
        if (s.TotalOrders < MinTotalOrders) return false;
        if (s.TotalSales < MinTotalSales) return false;
        if (s.ReturnRate > MaxReturnRate) return false;
        if (s.DisputeRate > MaxDisputeRate) return false;
        if (s.DefectRate > MaxDefectRate) return false;
        return true;
    }
}

public static class StoreLevelRules
{
    public static readonly Dictionary<StoreLevel, StoreLevelCriteria> Criteria =
        new()
        {
            [StoreLevel.Basic] = new StoreLevelCriteria
            {
                MinTotalOrders = 0,
                MinTotalSales = 0,
                MaxReturnRate = 1m,
                MaxDisputeRate = 1m,
                MaxDefectRate = 1m
            },
            [StoreLevel.Pro] = new StoreLevelCriteria
            {
                MinTotalOrders = 3,
                MinTotalSales = 1_000m,
                MaxReturnRate = 0.05m,
                MaxDisputeRate = 0.03m,
                MaxDefectRate = 0.05m
            },
            [StoreLevel.TopRated] = new StoreLevelCriteria
            {
                MinTotalOrders = 50,
                MinTotalSales = 2_000m,
                MaxReturnRate = 0.02m,
                MaxDisputeRate = 0.01m,
                MaxDefectRate = 0.02m
            }
        };

    // Level cao nhất mà store đạt được theo stats
    public static StoreLevel GetRecommendedLevel(StoreStats stats)
    {
        if (Criteria[StoreLevel.TopRated].Meets(stats))
            return StoreLevel.TopRated;

        if (Criteria[StoreLevel.Pro].Meets(stats))
            return StoreLevel.Pro;

        return StoreLevel.Basic;
    }

    // store hiện tại có đang đủ chuẩn level hiện tại không?
    public static bool MeetsCurrentLevel(StoreLevel level, StoreStats stats)
        => Criteria[level].Meets(stats);
}


