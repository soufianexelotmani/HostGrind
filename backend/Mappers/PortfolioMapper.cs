using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Entities.Document;
using HostGrid.Api.Entities.Property;

namespace HostGrid.Api.Mappers;

internal static class PortfolioMapper
{
    internal static void Apply(this Property property, PropertyWrite input)
    {
        property.TypeId = input.TypeId;
        property.Name = input.Name.Trim();
        property.Description = input.Description?.Trim();
        property.Address = input.Address.Trim();
        property.City = input.City.Trim();
        property.Latitude = input.Latitude;
        property.Longitude = input.Longitude;
        property.MaxGuests = input.MaxGuests;
        property.Bedrooms = input.Bedrooms;
        property.Bathrooms = input.Bathrooms;
        property.BaseNightlyRate = input.BaseNightlyRate;
        property.CheckInTime = input.CheckInTime;
        property.CheckOutTime = input.CheckOutTime;
    }

    internal static FileView File(Guid id, StoredFile file, int? order, bool readable)
        => new(id, file.OriginalName, file.ContentType, file.SizeBytes, file.UploadedAt, file.UploaderId, file.ScanStatus, order, readable);
}
