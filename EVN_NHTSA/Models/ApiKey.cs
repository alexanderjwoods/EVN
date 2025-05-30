namespace EVN_NHTSA.Models
{
    public class ApiKey
    {
        public required string Key { get; set; }
        public required DateTime ExpirationDate { get; set; }
    }
}