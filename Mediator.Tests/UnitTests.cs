namespace Mediator.Tests;

public class UnitTests
{
    [Fact]
    public void Unit_Value_IsDefault()
    {
        // Act
        var unit1 = Unit.Value;
        var unit2 = Unit.Value;
        var unit3 = default(Unit);

        // Assert
        Assert.Equal(unit1, unit2);
        Assert.Equal(unit1, unit3);
        Assert.Equal(unit2, unit3);
    }

    [Fact]
    public void Unit_Equals_WorksCorrectly()
    {
        // Arrange
        var unit1 = Unit.Value;
        var unit2 = Unit.Value;
        var unit3 = new Unit();

        // Act & Assert
        Assert.True(unit1.Equals(unit2));
        Assert.True(unit1.Equals(unit3));
        Assert.True(unit2.Equals(unit3));
        Assert.True(unit1.Equals((object)unit2));
    }

    [Fact]
    public void Unit_GetHashCode_IsConsistent()
    {
        // Arrange
        var unit1 = Unit.Value;
        var unit2 = Unit.Value;
        var unit3 = new Unit();

        // Act & Assert
        Assert.Equal(unit1.GetHashCode(), unit2.GetHashCode());
        Assert.Equal(unit1.GetHashCode(), unit3.GetHashCode());
    }

    [Fact]
    public void Unit_ToString_DoesNotThrow()
    {
        // Arrange
        var unit = Unit.Value;

        // Act & Assert
        var result = unit.ToString();
        Assert.NotNull(result);
    }

    [Fact]
    public void Unit_CanBeUsedInCollections()
    {
        // Arrange
        var units = new List<Unit> { Unit.Value, Unit.Value, new Unit() };

        // Act
        var distinctUnits = units.Distinct().ToList();

        // Assert
        Assert.Single(distinctUnits);
    }

    [Fact]
    public void Unit_CanBeUsedAsDictionaryKey()
    {
        // Arrange
        var dictionary = new Dictionary<Unit, string>();

        // Act
        dictionary[Unit.Value] = "test value";
        dictionary[new Unit()] = "overwritten value";

        // Assert
        Assert.Single(dictionary);
        Assert.Equal("overwritten value", dictionary[Unit.Value]);
    }

    [Fact]
    public void IRequest_MarkerInterface_InheritanceWorksCorrectly()
    {
        // This test verifies that IRequest properly inherits from IRequest<Unit>
        // Arrange & Act
        var request = new TestVoidRequest();

        // Assert
        Assert.IsAssignableFrom<IRequest<Unit>>(request);
        Assert.IsAssignableFrom<IRequest>(request);
    }

    private class TestVoidRequest : IRequest
    {
        // Empty implementation for testing inheritance
    }
}
