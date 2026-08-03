using System.ComponentModel.DataAnnotations;
using Marketplace.BLL.DTOs;

namespace Marketplace.Web.Models;

public class OrderCreateViewModel
{
    [Required(ErrorMessage = "Вкажіть країну")]
    [StringLength(100)]
    public string Country { get; set; } = "Україна";

    [Required(ErrorMessage = "Вкажіть місто")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть адресу доставки")]
    [StringLength(250)]
    public string Street { get; set; } = string.Empty;

    public List<CartItemDto> Items { get; set; } = new();
    public decimal Total => Items.Sum(item => item.LineTotal);
}
