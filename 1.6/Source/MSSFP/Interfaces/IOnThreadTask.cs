using Verse;

namespace MSSFP.Interfaces;

public interface IOnThreadTask : IExposable
{
    public void OnThreadTask(MSSFPGameManager manager);
}
