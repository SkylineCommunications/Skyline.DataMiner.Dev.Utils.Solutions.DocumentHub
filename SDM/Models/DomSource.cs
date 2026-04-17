namespace Skyline.DataMiner.DocumentHub.SDM.Models
{
    using Skyline.DataMiner.SDM;


	/// <summary>
	/// Configuration required to connect to a DOM Attachment file storage.
	/// </summary>
	//[GenerateExposers]
	//[SdmDomStorage("(slc)documenthub")]
	public class DomSource : SdmObject<DomSource>
	{
		/// <summary>
		/// Gets or sets unique identifier of the DOM Attachment source configuration.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the name of the DOM module where the attachments are stored.
		/// </summary>
		public string Module { get; set; }

		/// <summary>
		/// Gets or sets the name of the DOM definition where the attachments are stored.
		/// </summary>
		public string NetworkSharePath { get; set; }

		/// <summary>
		/// Gets or sets the name of the DOM field used to store the file attachments.
		/// </summary>
		public string Credential { get; set; }
	}
} 

