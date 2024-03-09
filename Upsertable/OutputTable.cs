using System.Threading.Tasks;

namespace Upsertable;

public class OutputTable(Output output)
{
    public async ValueTask DisposeAsync()
    {
        await output.DropTableAsync();
    }
}