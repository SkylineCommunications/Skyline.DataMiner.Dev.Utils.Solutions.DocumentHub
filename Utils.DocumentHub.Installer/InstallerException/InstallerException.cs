namespace Skyline.DataMiner.Utils.DocumentHub.Installer.InstallerException
{
	using System;
	using System.Runtime.Serialization;

	[Serializable]
	public class InstallerException : Exception
	{
		public InstallerException()
		{
		}

		public InstallerException(string message) : base(message)
		{
		}

		public InstallerException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected InstallerException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}