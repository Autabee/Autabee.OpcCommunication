﻿using System.Text.Json;

#if ANDROID
using Android.App;
using Android.Content.PM;
#endif
namespace Autabee.OpcScout
{
  public class DataFolder<T> : IPersistentProgramData<T>
    {
    string fileName;

    public DataFolder(string fileName)
    {
      this.fileName = fileName;
    }

    public T Load()
    {
      var path = Path.Combine("wwwroot/data", fileName + ".json");

      if (File.Exists(path))
      {
        var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
        return JsonSerializer.Deserialize<T>(json);
      }
      return default;
    }
    public void Save(T data)
    {
      var path = Path.Combine("data", fileName + ".json");
      var content = JsonSerializer.Serialize(data, typeof(T));
      File.WriteAllText(path, content);
    }
  }
}