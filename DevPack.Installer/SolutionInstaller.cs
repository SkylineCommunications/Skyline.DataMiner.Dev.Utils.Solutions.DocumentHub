namespace DevPack.Installer
{
	using System;
	using DevPack.Installer.DOM;
	using Skyline.AppInstaller;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.AppPackages;

	public class SolutionInstaller
	{
		private readonly IEngine engine;
		private readonly AppInstaller installer;

		public SolutionInstaller(IEngine engine, AppInstallContext context)
		{
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			installer = new AppInstaller(Engine.SLNetRaw, context);

			engine.GenerateInformation("Starting installation");
		}

		public void InstallDefaultContent()
		{
			installer.Log("Installing default content...");
			installer.InstallDefaultContent();
		}

		public void InstallDom()
		{
			installer.Log("Installing DOM...");
			var domInstaller = new DomInstaller(engine.GetUserConnection(), installer.Log);
			domInstaller.InstallDefaultContent();
		}
	}
}