using System.ComponentModel.DataAnnotations;
using ClientSphere.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClientSphere.ViewModels
{
    public class OrderCreateViewModel
    {
        [Required]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        public IEnumerable<SelectListItem>? CustomerList { get; set; }

        public List<OrderItemViewModel> Items { get; set; } = new List<OrderItemViewModel>();

        // For the "Add Item" UI
        public IEnumerable<SelectListItem>? ProductList { get; set; }
    }

    public class OrderItemViewModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
