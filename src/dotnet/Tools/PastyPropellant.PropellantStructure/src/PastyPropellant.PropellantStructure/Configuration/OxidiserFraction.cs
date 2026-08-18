using System.Text.Json.Serialization;
using UnitsNet;

namespace PastyPropellant.PropellantStructure.Configuration;

/// <summary>
/// One oxidiser fraction: a mass share and the size interval its particles are
/// drawn from.
/// </summary>
/// <remarks>
/// The mass shares of all fractions need not add up to one — four archived runs
/// come to 1.1 and 0.9885, and the model normalises internally. Treating that as an
/// error would reject reference runs, so it is a warning on the resolved record.
/// </remarks>
/// <param name="MassFraction">
/// <c>GDOK</c> of the original: this fraction's share of the oxidiser mass.
/// </param>
/// <param name="MinSize">Lower size bound; <c>DDOK(2i-1)</c>.</param>
/// <param name="MaxSize">Upper size bound; <c>DDOK(2i)</c>.</param>
/// <param name="FormsPockets">
/// <c>SFR</c> of the original. <see langword="false"/> takes the fraction out of
/// pocket formation and moves its mass into the homogenised oxidiser. Two of the 43
/// runs set it; everything else leaves every fraction participating.
/// </param>
public readonly record struct OxidiserFraction(
    double MassFraction,
    Length MinSize,
    Length MaxSize,
    bool FormsPockets = true)
{
    // Without this the deserialiser would take the struct's implicit parameterless
    // constructor and set the properties one by one, which turns an omitted
    // formsPockets into false - the opposite of the default written above, and a
    // fraction silently removed from pocket formation.
    [JsonConstructor]
    public OxidiserFraction(double massFraction, Length minSize, Length maxSize)
        : this(massFraction, minSize, maxSize, true)
    {
    }
}
