using System.Globalization;

namespace Messenger.Chats.Domain;

// A point on the map as a browser reports it. Unlike single-value objects it is stored
// as a nested document: a "lat,lon" string would have to be parsed back on every read.
public sealed record GeoPoint
{
    public const double MaxLatitude = 90;

    public const double MaxLongitude = 180;

    // Browsers report up to tens of kilometres when only the network position is known.
    public const double MaxAccuracyMeters = 100_000;

    public GeoPoint(
        double latitude,
        double longitude,
        double? accuracyMeters)
    {
        if (!double.IsFinite(latitude) || Math.Abs(latitude) > MaxLatitude)
            throw new DomainException($"Latitude must be from -{MaxLatitude} to {MaxLatitude}.");

        if (!double.IsFinite(longitude) || Math.Abs(longitude) > MaxLongitude)
            throw new DomainException($"Longitude must be from -{MaxLongitude} to {MaxLongitude}.");

        if (accuracyMeters != null && (!double.IsFinite(accuracyMeters.Value) || accuracyMeters < 0 || accuracyMeters > MaxAccuracyMeters))
            throw new DomainException($"Accuracy must be from 0 to {MaxAccuracyMeters} meters.");

        Latitude = latitude;
        Longitude = longitude;
        AccuracyMeters = accuracyMeters;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    public double? AccuracyMeters { get; }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Latitude},{Longitude}");
}
