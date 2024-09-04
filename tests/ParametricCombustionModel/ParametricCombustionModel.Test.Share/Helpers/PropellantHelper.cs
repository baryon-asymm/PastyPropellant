using System.Text.Json;
using ParametricCombustionModel.Core.Models;

namespace ParametricCombustionModel.Test.Share.Helpers;

public static class PropellantHelper
{
    public static IEnumerable<Propellant> GetPropellants()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Resources", "propellants.json");
        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var result = JsonSerializer.DeserializeAsync<List<Propellant>>(fileStream).Result;

        if (result is null)
            throw new InvalidOperationException("Deserialization failed.");

        return result.AsReadOnly();
    }
}
