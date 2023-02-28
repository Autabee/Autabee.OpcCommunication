using System.Text.Json;

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
			//generate path if not exists
			if (!Directory.Exists("wwwroot/data"))
			{
				Directory.CreateDirectory("wwwroot/data");
			}
			var path = Path.Combine("wwwroot/data", fileName + ".json");
			var json = JsonSerializer.Serialize(data);
			File.WriteAllText(path, json, System.Text.Encoding.UTF8);
		}
	}
}