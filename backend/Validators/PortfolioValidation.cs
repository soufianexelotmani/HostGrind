using FluentValidation;
using HostGrid.Api.DTOs.Properties;

namespace HostGrid.Api.Validators;

internal static class PortfolioValidation
{
    internal static void Register(IServiceCollection services)
    {
        SecurityValidationFilter.Add<OwnerWrite>(services, v =>
        {
            v.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            v.RuleFor(x => x.Email).MaximumLength(254).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
            v.RuleFor(x => x.Phone).MaximumLength(32);
        });
        SecurityValidationFilter.Add<PropertyWrite>(services, v =>
        {
            v.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            v.RuleFor(x => x.OwnerId).NotEmpty();
            v.RuleFor(x => x.TypeId).NotEmpty();
            v.RuleFor(x => x.City).NotEmpty().MaximumLength(120);
            v.RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
            v.RuleFor(x => x.Description).MaximumLength(4000);
            v.RuleFor(x => x.MaxGuests).GreaterThan((short)0);
            v.RuleFor(x => x.Bedrooms).GreaterThanOrEqualTo((short)0);
            v.RuleFor(x => x.Bathrooms).GreaterThanOrEqualTo((short)0);
            v.RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m).PrecisionScale(9, 6, true);
            v.RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m).PrecisionScale(9, 6, true);
            v.RuleFor(x => x.BaseNightlyRate).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true);
        });
        SecurityValidationFilter.Add<ContractWrite>(services, v =>
        {
            v.RuleFor(x => x.StartDate).NotEqual(DateOnly.MinValue);
            v.RuleFor(x => x.EndDate).Must((row, end) => end is null || end > row.StartDate);
            v.RuleFor(x => x.Rate).InclusiveBetween(0m, 100m).PrecisionScale(5, 2, true);
        });
        SecurityValidationFilter.Add<CommissionWrite>(services, v =>
        {
            v.RuleFor(x => x.EffectiveFrom).NotEqual(DateOnly.MinValue);
            v.RuleFor(x => x.Rate).InclusiveBetween(0, 100).PrecisionScale(5, 2, true);
        });
        SecurityValidationFilter.Add<InventoryWrite>(services, v =>
        {
            v.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            v.RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
            v.RuleFor(x => x.Condition).MaximumLength(120);
        });
        SecurityValidationFilter.Add<SeasonWrite>(services, v =>
        {
            v.RuleFor(x => x.StartDate).NotEqual(DateOnly.MinValue);
            v.RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
            v.RuleFor(x => x.Priority).InclusiveBetween((short)1, (short)3);
            v.RuleFor(x => x.DaysOfWeek).InclusiveBetween((short)1, (short)127)
                .Must((row, mask) => row.Priority != 1 || (mask & ~65) == 0);
            v.RuleFor(x => x.NightlyRate).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true);
        });
        SecurityValidationFilter.Add<ListingWrite>(services, v =>
        {
            v.RuleFor(x => x.ExternalReference).MaximumLength(200);
            v.RuleFor(x => x.Url).MaximumLength(2000).Must(url => string.IsNullOrWhiteSpace(url) ||
                Uri.TryCreate(url.Trim(), UriKind.Absolute, out var parsed) &&
                parsed.Scheme is "http" or "https" && parsed.UserInfo.Length == 0);
        });
        SecurityValidationFilter.Add<PhotoOrderWrite>(services, v => v.RuleFor(x => x.Ids).NotNull().Must(ids => ids.Length <= 1000));
        SecurityValidationFilter.Add<AmenitiesWrite>(services, v => v.RuleFor(x => x.Ids).NotNull().Must(ids => ids.Length <= 1000));
    }
}
