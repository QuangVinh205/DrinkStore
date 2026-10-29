using System.ComponentModel.DataAnnotations;

namespace DrinkStore.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        public string Address { get; set; } = "";

        [StringLength(100, ErrorMessage = "Mã giảm giá quá dài")]
        public string? CouponCode { get; set; }

        // Chỉ dùng để hiển thị; không nhận các giá trị tiền từ trình duyệt.
        public List<CartItemViewModel> CartItems { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
        public string? AppliedCouponCode { get; set; }
        public decimal Total => Math.Max(0, Subtotal - Discount);
    }
}
