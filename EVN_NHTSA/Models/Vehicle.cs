using System.Text.Json.Serialization;

namespace EVN_NHTSA.Models
{
    public class Vehicle : CosmosEntity
    {
        /// <summary>
        /// Identifier for the vehicle in the dealership's system.
        /// </summary>
        public required string DealerId { get; set; }

        /// <summary>
        /// Vehicles Vehicle Identification Number (VIN). Unique to each vehicle.
        /// </summary>
        public required string VIN { get; set; }

        /// <summary>
        /// Date and time when the vehicle's data was last modified.
        /// </summary>
        public required DateTime ModifiedDate { get; set; }

        /// <summary>
        /// Make of the vehicle.
        /// </summary>
        public string? Make { get; set; }

        /// <summary>
        /// Model of the vehicle.
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// Year of the vehicle.
        /// </summary>
        public string? Year { get; set; }

        /// <summary>
        /// Version or package of the vehicle.
        /// </summary>
        public string? Trim { get; set; }

        /// <summary>
        /// Type of the vehicle.
        /// </summary>
        public string? VehicleType { get; set; }

        /// <summary>
        /// Primary fuel type of the vehicle.
        /// </summary>
        public string? FuelTypePrimary { get; set; }
    }
}