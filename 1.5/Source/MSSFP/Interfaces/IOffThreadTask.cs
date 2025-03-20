using Verse;

namespace MSSFP.Interfaces;

public interface IOffThreadTask : IExposable
{
    public void OffThreadTask(MSSFPGameManager manager);
}
