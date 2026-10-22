namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	/// <summary>
	/// Declares the secrets required by the SharePoint storage type and builds the <see cref="SecretKey"/> for each of them.
	/// </summary>
	/// <remarks>
	/// Both the code that saves a secret (for example an Automation script) and the code that reads it
	/// (<c>SharePointHandler</c>) must use these helpers, so they always agree on the key and file location.
	/// Other storage types that need credentials should get an equivalent static class.
	/// </remarks>
	internal static class SharePointSecrets
	{
		/// <summary>
		/// Creates the key of the Azure AD client secret used to authenticate against a SharePoint configuration.
		/// </summary>
		/// <param name="configurationId">
		/// The identifier of the <c>SharePointConfiguration</c> the secret belongs to.
		/// Using the configuration identifier allows each configuration to have its own secret.
		/// </param>
		/// <returns>
		/// A <see cref="SecretKey"/> with storage type <see cref="StorageType.SharePoint"/> and name <c>clientsecret</c>,
		/// which maps to <c>sharepoint\&lt;configurationId&gt;\clientsecret.dat</c> in the file-based store.
		/// </returns>
		internal static SecretKey ClientSecret(Guid configurationId) => new SecretKey(StorageType.SharePoint, configurationId, "clientsecret");
	}
}
