using Messenger.Users.Domain;

namespace Messenger.Users.Tests.Domain;

public class UserProfileTests
{
    [Fact]
    public void Create_sets_name_and_trimmed_bio()
    {
        var userId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var profile = UserProfile.Create(userId, new DisplayName("Alice"), "  Hello!\n");

        Assert.Equal(userId, profile.UserId);
        Assert.Equal("Alice", profile.DisplayName.Value);
        Assert.Equal("Hello!", profile.Bio);
        Assert.Null(profile.AvatarId);
        Assert.InRange(profile.UpdatedAt, before, DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_bio_is_stored_as_null(string? bio)
    {
        var profile = UserProfile.Create(Guid.NewGuid(), new DisplayName("Alice"), bio);

        Assert.Null(profile.Bio);
    }

    [Fact]
    public void Bio_up_to_max_length_is_accepted_after_trimming()
    {
        var bio = new string('a', UserProfile.BioMaxLength);

        var profile = UserProfile.Create(Guid.NewGuid(), new DisplayName("Alice"), "  " + bio + "  ");

        Assert.Equal(bio, profile.Bio);
    }

    [Fact]
    public void Too_long_bio_is_rejected()
    {
        var bio = new string('a', UserProfile.BioMaxLength + 1);

        Assert.Throws<DomainException>(() => UserProfile.Create(Guid.NewGuid(), new DisplayName("Alice"), bio));
    }

    [Fact]
    public void Update_replaces_name_and_bio()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), new DisplayName("Alice"), "Old bio");
        var created = profile.UpdatedAt;

        profile.Update(new DisplayName("Alice Smith"), null);

        Assert.Equal("Alice Smith", profile.DisplayName.Value);
        Assert.Null(profile.Bio);
        Assert.True(profile.UpdatedAt >= created);
    }

    [Fact]
    public void Rejected_update_leaves_profile_unchanged()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), new DisplayName("Alice"), "Bio");
        var updatedAt = profile.UpdatedAt;

        Assert.Throws<DomainException>(() => profile.Update(
            new DisplayName("Bob"),
            new string('a', UserProfile.BioMaxLength + 1)));

        Assert.Equal("Alice", profile.DisplayName.Value);
        Assert.Equal("Bio", profile.Bio);
        Assert.Equal(updatedAt, profile.UpdatedAt);
    }
}
