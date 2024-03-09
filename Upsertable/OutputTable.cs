using System.Threading.Tasks;

namespace Upsertable.SqlServer;

public class OutputTable
{
    private readonly Output _output;

    public OutputTable(Output output)
    {
        _output = output;
    }

    public async ValueTask DisposeAsync()
    {
        await _output.DropTableAsync();
    }
}