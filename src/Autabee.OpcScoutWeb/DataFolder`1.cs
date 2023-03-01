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
			if (string.IsNullOrEmpty(fileName))
				throw new System.ArgumentException("File name cannot be empty", nameof(fileName));
			this.fileName = fileName;
		}

		public T? Load()
		{
			var path = Path.Combine("data", fileName + ".json");

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
			if (!Directory.Exists("data"))
			{
				Directory.CreateDirectory("data");
			}
			var path = Path.Combine("data", fileName + ".json");
			var json = JsonSerializer.Serialize(data);
			File.WriteAllText(path, json, System.Text.Encoding.UTF8);
		}
	}
}