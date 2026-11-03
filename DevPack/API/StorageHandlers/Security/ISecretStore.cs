namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	/// <summary>
	/// Read-side of the secret storage. This is the only part storage handlers get, so they can use a secret
	/// but cannot create, replace or delete one.
	/// </summary>
	/// <remarks>
	/// Secrets are never part of the public API surface: this interface and all its implementations are internal,
	/// and the value returned by <see cref="Read"/> must only be passed to the component that needs it (for example the
	/// credential of a storage client). It must never be returned to callers of the public API, logged, or put in exception messages.
	/// </remarks>
	internal interface ISecretReader
	{
		/// <summary>
		/// Reads the secret stored under the given key.
		/// </summary>
		/// <param name="key">The key that identifies the secret.</param>
		/// <returns>The plain-text secret.</returns>
		/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
		/// <exception cref="System.IO.FileNotFoundException">Thrown when the secret does not exist. Use <see cref="Exists"/> to check first.</exception>
		/// <exception cref="System.Security.Cryptography.CryptographicException">
		/// Thrown when the secret exists but cannot be decrypted, for example when it was saved on another machine.
		/// </exception>
		string Read(SecretKey key);

		/// <summary>
		/// Checks whether a secret is stored under the given key.
		/// </summary>
		/// <param name="key">The key that identifies the secret.</param>
		/// <returns><c>true</c> if the secret exists; otherwise, <c>false</c>. The value is not decrypted or validated.</returns>
		/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
		bool Exists(SecretKey key);
	}

	/// <summary>
	/// Write-side of the secret storage. It has no way to return a stored secret.
	/// </summary>
	internal interface ISecretWriter
	{
		/// <summary>
		/// Saves the secret, replacing any existing value stored under the same key.
		/// </summary>
		/// <param name="key">The key that identifies the secret.</param>
		/// <param name="value">The plain-text secret to store. The store is responsible for protecting it.</param>
		/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
		/// <exception cref="System.ArgumentException">Thrown when <paramref name="value"/> is null or empty.</exception>
		/// <remarks>
		/// The write should be atomic: a concurrent <see cref="ISecretReader.Read"/> must see either the old or the new value, never a partial one.
		/// </remarks>
		void Save(SecretKey key, string value);

		/// <summary>
		/// Deletes the secret stored under the given key. Does nothing if it does not exist.
		/// </summary>
		/// <param name="key">The key that identifies the secret.</param>
		/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
		void Delete(SecretKey key);
	}

	/// <summary>
	/// Stores and retrieves secrets (passwords, client secrets, tokens) used by storage handlers.
	/// </summary>
	/// <remarks>
	/// Storage types that need credentials use this abstraction instead of handling encryption and file locations themselves.
	/// Each secret is addressed by a <see cref="SecretKey"/>. Storage types that need no credentials (Local, DOM) do not use it.
	/// Consumers should depend on <see cref="ISecretReader"/> or <see cref="ISecretWriter"/> instead of this combined interface.
	/// <para>
	/// Implementations must never log or expose secret values, and must treat a secret that was saved
	/// under one <see cref="SecretKey"/> as unreadable under any other key.
	/// </para>
	/// </remarks>
	internal interface ISecretStore : ISecretReader, ISecretWriter
	{
	}
}
