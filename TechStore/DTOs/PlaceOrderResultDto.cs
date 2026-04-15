namespace TechStore.DTOs
{
    public class PlaceOrderResultDto
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int OrderId { get; set; }
    }
}