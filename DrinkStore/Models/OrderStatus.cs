namespace DrinkStore.Models
{
    public enum OrderStatus
    {
        Pending = 0,      // Chờ xác nhận
        Confirmed = 1,    // Đã xác nhận
        Preparing = 2,    // Đang chuẩn bị
        Completed = 3,    // Hoàn thành
        Cancelled = 4     // Đã hủy
    }
}