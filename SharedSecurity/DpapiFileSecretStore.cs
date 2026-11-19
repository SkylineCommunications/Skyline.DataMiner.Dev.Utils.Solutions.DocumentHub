namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using System.Text;

	/// <summary>Migration-only reader for files written by older DocumentHub versions. Never used during normal retrieval.</summary>
	[SuppressMessage("Usage", "SLC_SC0002:File operation usage detected without secure path construction or validation", Justification = "Paths are built only from the fixed root folder and a SecretKey, whose storage type and GUID scope are not free text and whose name is restricted to [a-z0-9_-]+. No caller-supplied path segment can reach the file system.")]
	internal sealed class DpapiFileSecretStore : ISecretStore
	{
		internal const string DefaultRootPath = @"C:\Skyline DataMiner\Security\DocumentHub";

		private readonly string _rootPath;
		private readonly Dictionary<string, string> _snapshots = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		internal DpapiFileSecretStore(string rootPath = DefaultRootPath)
		{
			if (string.IsNullOrWhiteSpace(rootPath))
			{
				throw new ArgumentNullException(nameof(rootPath));
			}

			_rootPath = rootPath;
		}

		internal bool GlobalExists => File.Exists(Path.Combine(_rootPath, "graphsecret.dat"));

		public void Save(SecretKey key, string value)
		{
			throw new NotSupportedException("Legacy DPAPI storage is read-only. Provision a native credential instead.");
		}

		public string Read(SecretKey key)
		{
			return ReadPath(GetPath(key), Encoding.UTF8.GetBytes(key.Entropy));
		}

		public bool Exists(SecretKey key) => File.Exists(GetPath(key));

		public void Delete(SecretKey key) => DeleteSnapshot(GetPath(key));

		internal string ReadGlobal() => ReadPath(Path.Combine(_rootPath, "graphsecret.dat"), null);

		internal void DeleteGlobal() => DeleteSnapshot(Path.Combine(_rootPath, "graphsecret.dat"));

		private static void RejectReparsePoints(string path)
		{
			if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
				throw new IOException("Legacy migration does not follow reparse points.");

			for (var directory = new DirectoryInfo(Path.GetDirectoryName(path)); directory != null; directory = directory.Parent)
			{
				if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
					throw new IOException("Legacy migration does not follow reparse points.");
			}
		}

		private string ReadPath(string path, byte[] entropy)
		{
			RejectReparsePoints(path);
			if (File.Exists(path + ".tmp"))
				throw new IOException("An unfinished legacy write exists. Stop the old DocumentHub version before migrating.");

			var encrypted = File.ReadAllText(path);
			var secret = DPAPIHelper.Decrypt(encrypted, entropy);
			_snapshots[path] = encrypted;
			return secret;
		}

		private void DeleteSnapshot(string path)
		{
			if (!_snapshots.TryGetValue(path, out var encrypted))
				throw new InvalidOperationException("The legacy file was not read and verified by this migration.");

			RejectReparsePoints(path);
			var retired = path + "." + Guid.NewGuid().ToString("N") + ".migrated";
			File.Move(path, retired);
			try
			{
				if (File.ReadAllText(retired) != encrypted)
					throw new IOException("The legacy file changed during migration. The encrypted file has been retained.");

				File.Delete(retired);
			}
			catch
			{
				if (!File.Exists(path) && File.Exists(retired))
					File.Move(retired, path);
				throw;
			}

			if (File.Exists(path) || File.Exists(path + ".tmp"))
				throw new IOException("Another legacy write was detected. Stop the old version and repeat migration.");
		}

		private string GetPath(SecretKey key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			return Path.Combine(_rootPath, key.RelativePath);
		}
	}
}
