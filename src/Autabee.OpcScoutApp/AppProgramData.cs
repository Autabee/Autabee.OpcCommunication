using System.Text.Json;
using Autabee.OpcScout;

#if ANDROID
using Android.App;
using Android.Content.PM;
#endif
namespace Autabee.OpcScout
{
  public class AppProgramData<T> : IPersistentProgramData<T>
    {
    string fileName;

    public AppProgramData(string fileName)
    {
      if (string.IsNullOrEmpty(fileName))
        throw new System.ArgumentException("File name cannot be empty", nameof(fileName));
      this.fileName = fileName;
    }

    public T Load()
    {
      var path = Path.Combine(FileSystem.Current.AppDataDirectory, fileName + ".json");

      if (File.Exists(path))
      {
        var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
        return JsonSerializer.Deserialize<T>(json);
      }
      return default;
    }
    public void Save(T data)
    {
      var path = Path.Combine(FileSystem.Current.AppDataDirectory, fileName + ".json");
      var content = JsonSerializer.Serialize(data, typeof(T));
      File.WriteAllText(path, content);
    }
  }
}