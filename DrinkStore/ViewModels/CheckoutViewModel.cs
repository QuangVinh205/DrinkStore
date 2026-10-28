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

        public List<CartItemViewModel> CartItems { get; set; } = new();

        public decimal Total => CartItems.Sum(x => x.TotalPrice);
    }
}