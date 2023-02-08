#if ANDROID
using Android.App;
using Android.Content.PM;
#endif
namespace Autabee.OpcScoutApp
{
  public interface IPersistentProgramData<T>
  {
    T Load();
    void Save(T data);
  }
}