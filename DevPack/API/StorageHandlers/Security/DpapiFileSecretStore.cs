namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using System.Text;

	/// <summary>
	/// Stores secrets as DPAPI (machine scope) encrypted files below a root folder.
	/// </summary>
	[SuppressMessage("Usage", "SLC_SC0002:File operation usage detected without secure path construction or validation", Justification = "Paths are built only from the fixed root folder and a SecretKey, whose storage type and GUID scope are not free text and whose name is restricted to [a-z0-9_-]+. No caller-supplied path segment can reach the file system.")]
	internal sealed class DpapiFileSecretStore : ISecretStore
	{
		internal const string DefaultRootPath = @"C:\Skyline DataMiner\Security\DocumentHub";

		private readonly string _rootPath;

		internal DpapiFileSecretStore(string rootPath = DefaultRootPath)
		{
			if (string.IsNullOrWhiteSpace(rootPath))
			{
				throw new ArgumentNullException(nameof(rootPath));
			}

			_rootPath = rootPath;
		}

		public void Save(SecretKey key, string value)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentException("Secret value cannot be null or empty.", nameof(value));
			}

			string path = GetPath(key);
			string tempPath = path + ".tmp";
			string encrypted = DPAPIHelper.Encrypt(value, GetEntropy(key));

			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(tempPath, encrypted);

			// Atomic replace so readers never see a partially written file.
			if (File.Exists(path))
			{
				File.Replace(tempPath, path, null);
			}
			else
			{
				File.Move(tempPath, path);
			}
		}

		public string Read(SecretKey key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			string path = GetPath(key);
			if (!File.Exists(path))
			{
				throw new FileNotFoundException($"Secret '{key}' not found.", path);
			}

			return DPAPIHelper.Decrypt(File.ReadAllText(path), GetEntropy(key));
		}

		public bool Exists(SecretKey key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			return File.Exists(GetPath(key));
		}

		public void Delete(SecretKey key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			string path = GetPath(key);
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}

		private static byte[] GetEntropy(SecretKey key) => Encoding.UTF8.GetBytes(key.Entropy);

		private string GetPath(SecretKey key) => Path.Combine(_rootPath, key.RelativePath);
	}
}
