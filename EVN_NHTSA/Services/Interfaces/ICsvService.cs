namespace EVN_NHTSA.Services
{
    public interface ICsvService
    {
        (List<T>, string) GetCsvDataFromStream<T>(Stream stream, string[]? validHeaders = null);
        Task<byte[]> WriteRecordsToStreamAsync<T>(List<T> records);
    }
}