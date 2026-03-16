namespace HomeBuddy_API.DTOs.Requests.GroupDTOs
{
    /// <summary>
    /// Request to apply a percentage discount to all variants in a product group.
    /// Current Price becomes ListPrice (original); Price is set to Price * (1 - DiscountPercent/100).
    /// </summary>
    public class ApplyGroupDiscountRequest
    {
        /// <summary>Discount percentage, 1–99.</summary>
        public int DiscountPercent { get; set; }
    }
}
