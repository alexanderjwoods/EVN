using System.Text.RegularExpressions;
using CsvHelper.Configuration;
using EVN_NHTSA.Models;

namespace EVN_NHTSA.Mappers
{
    /// <summary>
	/// Mapping for CSV import of vehicles.
	/// </summary>
	public sealed partial class VehicleMap : ClassMap<Vehicle>
    {
        public VehicleMap()
        {
            Map(m => m.DealerId).Name("dealerId")
                .Validate(field => !string.IsNullOrWhiteSpace(field.Field),
                field => "DealerId is required.");

            Map(m => m.VIN).Name("vin")
                .Validate(field => VinValidationRegex().IsMatch(field.Field.ToUpper()),
                field => $"{field.Field} is not a valid VIN.");

            Map(m => m.ModifiedDate).Name("modifiedDate")
                .Validate(field => DateTime.TryParse(field.Field, out _),
                field => $"{field.Field} is not a valid date.");

            Map(m => m.Make).Ignore();
            Map(m => m.Model).Ignore();
            Map(m => m.Year).Ignore();
            Map(m => m.Trim).Ignore();
            Map(m => m.VehicleType).Ignore();
            Map(m => m.FuelTypePrimary).Ignore();
        }

        [GeneratedRegex(@"^[A-HJ-NPR-Z0-9]{17}$")]
        private static partial Regex VinValidationRegex();
    }
}