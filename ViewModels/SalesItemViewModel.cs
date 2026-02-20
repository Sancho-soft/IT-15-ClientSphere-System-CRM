using System;

namespace ClientSphere.ViewModels
{
    public class SalesItemViewModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
    }
}
