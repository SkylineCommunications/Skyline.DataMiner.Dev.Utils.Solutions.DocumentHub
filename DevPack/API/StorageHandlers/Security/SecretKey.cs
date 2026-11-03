namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;

	/// <summary>
	/// Identifies a single secret by a source, zero or more scope folders and a name,
	/// which maps to <c>source\[scope\...]name.dat</c>.
	/// </summary>
	/// <remarks>
	/// The source identifies the kind of credential, not necessarily a storage type
	/// (for example <c>sharepoint</c> for app registrations and <c>sharepointuser</c> for user accounts).
	/// Scopes group secrets (for example by tenant). Keys should only be created through
	/// source-specific helpers such as <see cref="SharePointSecrets"/>, so the layout of each source is defined in one place.
	/// </remarks>
	internal sealed class SecretKey
	{
		private static readonly Regex SegmentPattern = new Regex("^[a-z0-9_-]+$", RegexOptions.Compiled);

		/// <summary>
		/// Initializes a new instance of the <see cref="SecretKey"/> class.
		/// </summary>
		/// <param name="source">The source folder, for example <c>sharepoint</c>.</param>
		/// <param name="name">The file name without extension, for example a client ID or <c>token</c>.</param>
		/// <param name="scopes">The optional scope folders between source and name, for example a tenant ID.</param>
		/// <exception cref="ArgumentException">
		/// Thrown when a segment is null, empty or contains characters other than lowercase letters, digits, '-' or '_'.
		/// </exception>
		internal SecretKey(string source, string name, params string[] scopes)
		{
			Source = Validate(source, nameof(source));
			Name = Validate(name, nameof(name));
			Scopes = (scopes ?? new string[0]).Select(s => Validate(s, nameof(scopes))).ToList().AsReadOnly();
		}

		/// <summary>
		/// Gets the source folder.
		/// </summary>
		internal string Source { get; }

		/// <summary>
		/// Gets the scope folders between the source and the name; empty when the secret is global for the source.
		/// </summary>
		internal IReadOnlyList<string> Scopes { get; }

		/// <summary>
		/// Gets the secret name.
		/// </summary>
		internal string Name { get; }

		/// <summary>
		/// Gets the relative path (without root) of the secret: <c>source\[scope\...]name.dat</c>.
		/// </summary>
		internal string RelativePath => String.Join("\\", new[] { Source }.Concat(Scopes).Concat(new[] { Name + ".dat" }));

		/// <summary>
		/// Gets the value bound to the encryption so a secret file cannot be reused under another key.
		/// </summary>
		internal string Entropy => String.Join("|", new[] { "DocumentHub", Source }.Concat(Scopes).Concat(new[] { Name }));

		public override string ToString() => RelativePath;

		private static string Validate(string segment, string paramName)
		{
			if (String.IsNullOrEmpty(segment) || !SegmentPattern.IsMatch(segment))
			{
				throw new ArgumentException("Secret key segments must be non-empty and only contain lowercase letters, digits, '-' or '_'.", paramName);
			}

			return segment;
		}
	}
}
