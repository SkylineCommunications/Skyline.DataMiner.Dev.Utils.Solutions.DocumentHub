namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using System.Text.RegularExpressions;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	/// <summary>
	/// Identifies a single secret: the storage type it belongs to, the scope (for example a configuration or user identifier) and a name.
	/// </summary>
	internal sealed class SecretKey
	{
		private static readonly Regex NamePattern = new Regex("^[a-z0-9_-]+$", RegexOptions.Compiled);

		internal SecretKey(StorageType storage, Guid scope, string name)
		{
			if (string.IsNullOrEmpty(name) || !NamePattern.IsMatch(name))
			{
				throw new ArgumentException("Secret name must be non-empty and only contain lowercase letters, digits, '-' or '_'.", nameof(name));
			}

			Storage = storage;
			Scope = scope;
			Name = name;
		}

		/// <summary>
		/// Gets the storage type the secret belongs to.
		/// </summary>
		internal StorageType Storage { get; }

		/// <summary>
		/// Gets the scope identifier. <see cref="Guid.Empty"/> means the secret is global for the storage type.
		/// </summary>
		internal Guid Scope { get; }

		/// <summary>
		/// Gets the secret name.
		/// </summary>
		internal string Name { get; }

		/// <summary>
		/// Gets the relative path (without root) of the secret: storage\[scope\]name.dat.
		/// </summary>
		internal string RelativePath
		{
			get
			{
				string storage = Storage.ToString().ToLowerInvariant();
				string file = Name + ".dat";
				return Scope == Guid.Empty
					? storage + "\\" + file
					: storage + "\\" + Scope.ToString("D") + "\\" + file;
			}
		}

		/// <summary>
		/// Gets the value bound to the encryption so a secret file cannot be reused under another key.
		/// </summary>
		internal string Entropy => $"DocumentHub|{Storage.ToString().ToLowerInvariant()}|{Scope:D}|{Name}";

		public override string ToString() => RelativePath;
	}
}
