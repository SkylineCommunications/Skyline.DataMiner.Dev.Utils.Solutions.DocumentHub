namespace Skyline.DataMiner.Solutions.DocumentHub.SDM.Models
{
	using Skyline.DataMiner.SDM;

	/// <summary>
	/// Represents a document classification that defines how and where files are stored.
	/// </summary>
	// [GenerateExposers]
	// [SdmDomStorage("(slc)documenthub")]
	public class DocumentBucket : SdmObject<DocumentBucket>
	{
		/// <summary>
		/// Gets or sets human readable name of the bucket.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets optional description explaining the purpose of the bucket.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Gets or sets base upload path used by the storage handler.
		/// This path is interpreted relative to the storage backend.
		/// </summary>
		public string UploadPath { get; set; }

		/// <summary>
		/// Gets or sets storage backend used by this bucket.
		/// </summary>
		public StorageType StorageType { get; set; }

		/// <summary>
		/// Gets or sets allowed file extensions for this bucket.
		/// Typically provided as a comma separated list without dots.
		/// Example. pdf,docx,xlsx
		/// </summary>
		public string Extensions { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether indicates whether this bucket is the default bucket.
		/// </summary>
		public bool IsDefault { get; set; }

		/// <summary>
		/// Gets or sets a reference to the associated DOM Source with this bucket.
		/// </summary>
		public SdmObjectReference<DomSource> DOMSource { get; set; }

		/// <summary>
		/// Gets or sets a string value indicating the definition associated with this bucket.
		/// </summary>
		public string Definition { get; set; }

		/// <summary>
		/// Gets or sets the maximum allowed file size for this bucket, in bytes.
		/// </summary>
		public long SizeLimit { get; set; }
	}
}