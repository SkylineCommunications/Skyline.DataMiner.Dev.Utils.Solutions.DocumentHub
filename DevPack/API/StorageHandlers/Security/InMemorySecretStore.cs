namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System.Collections.Generic;
	using System.IO;

	/// <summary>
	/// Non-persistent <see cref="ISecretStore"/>, intended for tests.
	/// </summary>
	internal sealed class InMemorySecretStore : ISecretStore
	{
		private readonly Dictionary<string, string> _secrets = new Dictionary<string, string>();

		public void Save(SecretKey key, string value) => _secrets[key.RelativePath] = value;

		public string Read(SecretKey key)
		{
			if (!_secrets.TryGetValue(key.RelativePath, out string value))
			{
				throw new FileNotFoundException($"Secret '{key}' not found.");
			}

			return value;
		}

		public bool Exists(SecretKey key) => _secrets.ContainsKey(key.RelativePath);

		public void Delete(SecretKey key) => _secrets.Remove(key.RelativePath);
	}
}
