namespace PRN222_Group3.Models
{
    public class StoreStats
    {
        public int TotalOrders { get; set; }      // số đơn hoàn tất
        public decimal TotalSales { get; set; }   // tổng doanh thu (VD 90 ngày)
        public decimal ReturnRate { get; set; }   // 0.03 = 3%
        public decimal DisputeRate { get; set; }  // 0.01 = 1%
        public decimal DefectRate { get; set; }   // 0.04 = 4%
    }

    public enum StoreLevel : byte
    {
        Basic = 1,
        Pro = 2,
        TopRated = 3
    }

    public class AdminStoreListItemVm
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = default!;
        public StoreLevel CurrentLevel { get; set; }

        public int TotalOrders { get; set; }
        public decimal TotalSales { get; set; }
        public decimal ReturnRate { get; set; }
        public decimal DisputeRate { get; set; }
        public decimal DefectRate { get; set; }

        public bool CanUpgrade { get; set; }
        public bool CanDowngrade { get; set; }
        public StoreLevel TargetLevel { get; set; }   // level đề xuất (up hoặc down)
    }


}
