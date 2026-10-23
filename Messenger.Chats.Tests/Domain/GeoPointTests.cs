using Messenger.Chats.Domain;

namespace Messenger.Chats.Tests.Domain;

public class GeoPointTests
{
    [Theory]
    [InlineData(55.7558, 37.6173, 12.5)]
    [InlineData(-90, -180, null)]
    [InlineData(90, 180, 0.0)]
    public void Point_inside_the_ranges_is_kept_as_is(double latitude, double longitude, double? accuracy)
    {
        var point = new GeoPoint(latitude, longitude, accuracy);

        Assert.Equal(latitude, point.Latitude);
        Assert.Equal(longitude, point.Longitude);
        Assert.Equal(accuracy, point.AccuracyMeters);
    }

    [Theory]
    [InlineData(90.0001, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 180.5)]
    [InlineData(0, -181)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.PositiveInfinity)]
    public void Point_outside_the_ranges_is_rejected(double latitude, double longitude)
    {
        Assert.Throws<DomainException>(() => new GeoPoint(latitude, longitude, null));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(GeoPoint.MaxAccuracyMeters + 1)]
    [InlineData(double.NaN)]
    public void Accuracy_outside_the_range_is_rejected(double accuracy)
    {
        Assert.Throws<DomainException>(() => new GeoPoint(0, 0, accuracy));
    }

    [Fact]
    public void ToString_does_not_depend_on_the_culture()
    {
        var culture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("ru-RU");

        try
        {
            Assert.Equal("55.75,37.61", new GeoPoint(55.75, 37.61, null).ToString());
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = culture;
        }
    }
}
