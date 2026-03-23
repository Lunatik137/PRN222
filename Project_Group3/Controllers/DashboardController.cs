using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN222_Group3.Models;
using PRN222_Group3.Repository;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace PRN222_Group3.Controllers
{
    [Authorize(Roles = "SuperAdmin,Support,Moderator,Ops")]
    public class DashboardController : Controller
    {
        private readonly StatisticsRepository _statisticsRepo;

        public DashboardController()
        {
            _statisticsRepo = new StatisticsRepository();
        }


        public async Task<IActionResult> Dashboard(DateTime? startDate, DateTime? endDate, string groupBy = "month", int page = 1)
        {
            var now = DateTime.UtcNow.AddHours(7);
            DateTime sDate = startDate ?? new DateTime(now.Year, now.Month, 1);
            DateTime eDate = endDate ?? sDate.AddMonths(1).AddDays(-1);

            var statistics = await _statisticsRepo.GetDashboardStatistics(sDate, eDate, groupBy, page, 10);

            ViewBag.StartDate = sDate.ToString("yyyy-MM-dd");
            ViewBag.EndDate = eDate.ToString("yyyy-MM-dd");
            ViewBag.GroupBy = groupBy;
            ViewBag.Statistics = statistics;

            return View("Dashboard");
        }

        [Authorize(Roles = "SuperAdmin,Ops")]
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(DateTime? startDate, DateTime? endDate, string groupBy = "month")
        {
            var now = DateTime.UtcNow.AddHours(7);
            DateTime sDate = startDate ?? new DateTime(now.Year, now.Month, 1);
            DateTime eDate = endDate ?? sDate.AddMonths(1).AddDays(-1);

            var statistics = await _statisticsRepo.GetDashboardStatistics(sDate, eDate, groupBy, 1, 10);

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Dashboard Statistics");

                ws.Cell(1, 1).Value = "DASHBOARD STATISTICS";
                ws.Cell(2, 1).Value = $"From: {sDate:dd/MM/yyyy}  -  To: {eDate:dd/MM/yyyy}";
                ws.Range("A1:B1").Merge().Style.Font.SetBold().Font.FontSize = 16;
                ws.Range("A2:B2").Merge().Style.Font.Italic = true;

                ws.Cell(4, 1).Value = "Metric";
                ws.Cell(4, 2).Value = "Value";
                ws.Range("A4:B4").Style.Font.SetBold().Fill.BackgroundColor = XLColor.AliceBlue;

                ws.Cell(5, 1).Value = "Total Revenue";
                ws.Cell(5, 2).Value = statistics.Revenue;

                ws.Cell(6, 1).Value = "Total Orders";
                ws.Cell(6, 2).Value = statistics.Orders;

                ws.Cell(7, 1).Value = "Total Users";
                ws.Cell(7, 2).Value = statistics.TotalUsers;

                ws.Cell(8, 1).Value = "New Users";
                ws.Cell(8, 2).Value = statistics.NewUsers;

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"Dashboard_Statistics_{sDate:yyyyMMdd}_{eDate:yyyyMMdd}.xlsx"
                    );
                }
            }
        }
    }
}