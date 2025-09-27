using System.Collections.ObjectModel;
using PastyPropellant.Core.Utils;
using PastyPropellant.RegionMapper.Models;

namespace PastyPropellant.RegionMapper.Interfaces;

public interface IRegionMapper
{
    public Task<OperationResult<ReadOnlyCollection<PropellantRegionMapped>>> MapRegionAsync(
        string propellantsFilePath,
        string componentsFilePath);
}
