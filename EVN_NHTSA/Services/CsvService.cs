using CsvHelper;
using CsvHelper.Configuration;
using EVN_NHTSA.Models;

namespace EVN_NHTSA.Services
{
    public class CsvService(CsvConfiguration csvConfiguration) : ICsvService
    {
        public (List<T>, string) GetCsvDataFromStream<T>(Stream stream, string[]? validHeaders = null)
        {
            using var reader = new StreamReader(stream);
            using var csvReader = new CsvReader(reader, csvConfiguration);

			if (!csvReader.Read() || !csvReader.ReadHeader())
			{
				throw new InvalidOperationException("Unable to read CSV header or file is empty.");
			}

			var headers = csvReader.HeaderRecord;
			if (headers is null || (validHeaders is not null && headers.SequenceEqual(validHeaders)))
			{
				throw new HeaderValidationException(
					csvReader.Context,
					[],
					$"Expected headers: '{string.Join(",", validHeaders!)}'. Found headers: '{string.Join(",", headers ?? [])}'");
			}

			return (csvReader.GetRecords<T>().ToList(), string.Empty);
        }

        /// <inheritdoc/>
        public async Task<byte[]> WriteRecordsToStreamAsync<T>(List<T> records)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);
            using var csv = new CsvWriter(writer, csvConfiguration);
            {
                await csv.WriteRecordsAsync(records);
            }

            var bytes = memoryStream.ToArray();

            return bytes;
		}
    }
}