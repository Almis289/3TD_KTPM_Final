namespace Book_Store.Models
{
    public class ProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string Description { get; set; }
        public string Publisher { get; set; }
        public int PageCount { get; set; }
        public string Language { get; set; }
    }
}
