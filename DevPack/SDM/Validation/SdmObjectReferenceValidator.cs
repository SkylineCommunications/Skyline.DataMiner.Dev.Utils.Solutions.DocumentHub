namespace Skyline.DataMiner.Solutions.DocumentHub.SDM.Validation
{
	using System;
	using Skyline.DataMiner.SDM;

	internal static class SdmObjectReferenceValidator
	{
		/// <summary>
		/// Checks if the given <see cref="SdmObjectReference{T}"/> is valid by verifying that its identifier is a non-empty string and can be parsed as a <see cref="Guid"/>.
		/// </summary>
		/// <typeparam name="T">SDM object type.</typeparam>
		/// <param name="reference">The <see cref="SdmObjectReference{T}"/> to validate.</param>
		/// <param name="identifierGuid">Guid returned if the validation succeeds.</param>
		/// <returns><see langword="true"/> if the object reference can be parsed as a <see cref="Guid"/> (or <see cref="Guid.Empty"/>), <see langword="false"/> otherwise.</returns>
		internal static bool IsValidReference<T>(this SdmObjectReference<T> reference, out Guid identifierGuid) where T : SdmObject<T>
		{
			identifierGuid = default;
			var referenceIdentifier = reference.Identifier;
			return !string.IsNullOrWhiteSpace(referenceIdentifier) && Guid.TryParse(referenceIdentifier, out identifierGuid) && identifierGuid != Guid.Empty;
		}
	}
}
