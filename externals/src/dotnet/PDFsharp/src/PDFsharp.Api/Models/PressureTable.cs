using System.Collections.ObjectModel;

namespace PDFsharp.Api.Models;

public class PressureTable
{
    public ReadOnlyCollection<double> ColumnProportions { get; private set; }
    public ReadOnlyCollection<ReadOnlyCollection<string>> Rows { get; private set; }

    public PressureTable(IEnumerable<double> columnProportions, IEnumerable<IEnumerable<string>> rows)
    {
        ColumnProportions = columnProportions.ToArray().AsReadOnly();
        Rows = rows.Select(x => x.ToArray().AsReadOnly()).ToArray().AsReadOnly();
    }
}
