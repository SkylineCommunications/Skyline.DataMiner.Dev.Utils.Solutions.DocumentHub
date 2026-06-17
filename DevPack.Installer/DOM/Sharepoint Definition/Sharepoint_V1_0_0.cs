namespace Skyline.DataMiner.Solutions.DocumentHub.Installer.DOM.Sharepoint_Definition
{
	using System;
	using System.Collections.Generic;
	using DevPack.Installer.DOM;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.GenericEnums;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Utils.DOM.Builders;

	internal class Sharepoint_V1_0_0 : DomMigration
	{
		public Sharepoint_V1_0_0(IConnection connection, Action<string> logMethod = null) : base(connection, SharePointConfigurationDomMapper.ModuleId, logMethod)
		{
		}

		public override void Migrate()
		{
			var section = new SectionDefinitionBuilder()
				.WithID(SharePointConfigurationDomMapper.SharePointConfigurationProperties.SectionDefinitionId)
				.WithName(nameof(SharePointConfigurationDomMapper.SharePointConfigurationProperties))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(SharePointConfigurationDomMapper.SharePointConfigurationProperties.ClientID)
					.WithName(nameof(SharePointConfigurationDomMapper.SharePointConfigurationProperties.ClientID))
					.WithType(typeof(string))
					.WithIsOptional(true)
					.WithTooltip("The client ID of the Azure AD application used for SharePoint authentication"))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(SharePointConfigurationDomMapper.SharePointConfigurationProperties.Name)
					.WithName(nameof(SharePointConfigurationDomMapper.SharePointConfigurationProperties.Name))
					.WithType(typeof(string))
					.WithIsOptional(true)
					.WithTooltip("The name associated to the SharePoint configuration"))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(SharePointConfigurationDomMapper.SharePointConfigurationProperties.SiteURL)
					.WithName(nameof(SharePointConfigurationDomMapper.SharePointConfigurationProperties.SiteURL))
					.WithType(typeof(string))
					.WithIsOptional(true)
					.WithTooltip("The URL of the SharePoint site (e.g., https://abc.sharepoint.com/sites/sitename)"))
 				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(SharePointConfigurationDomMapper.SharePointConfigurationProperties.DocumentLibraryName)
					.WithType(typeof(string))
					.WithName(nameof(SharePointConfigurationDomMapper.SharePointConfigurationProperties.DocumentLibraryName))
					.WithIsOptional(true)
					.WithTooltip("The name of the SharePoint document library where files will be stored"))
				.AddFieldDescriptor(new GenericEnumFieldDescriptorBuilder()
					.WithID(SharePointConfigurationDomMapper.SharePointConfigurationProperties.Status)
					.WithName(nameof(SharePointConfigurationDomMapper.SharePointConfigurationProperties.Status))
					.WithEnumType(GenericEnumFieldDescriptorBuilder.EnumType.Int)
					.AddEnumValue(new GenericEnumEntry<int>("Disconnected", 0))
					.AddEnumValue(new GenericEnumEntry<int>("Connected", 1))
					.WithIsOptional(true)
					.WithTooltip("The current connection status to the SharePoint site"))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(SharePointConfigurationDomMapper.SharePointConfigurationProperties.TenantID)
					.WithType(typeof(string))
					.WithName(nameof(SharePointConfigurationDomMapper.SharePointConfigurationProperties.TenantID))
					.WithIsOptional(true)
					.WithTooltip("The Azure AD tenant ID for SharePoint authentication"))
				.Build();

			Import(SectionDefinitionExposers.ID.Equal(SharePointConfigurationDomMapper.SharePointConfigurationProperties.SectionDefinitionId), section);

			var sharepointDefinition = new DomDefinitionBuilder()
				.WithID(SharePointConfigurationDomMapper.DomDefinitionId)
				.WithName("SharePoint")
				.AddSectionDefinitionLink(new Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions.SectionDefinitionLink
				{
					SectionDefinitionID = SharePointConfigurationDomMapper.SharePointConfigurationProperties.SectionDefinitionId,
					AllowMultipleSections = false,
					IsOptional = false,
					IsSoftDeleted = false,
				})
				.Build();

			// Ensure ModuleSettingsOverrides is not null before setting NameDefinition
			if (sharepointDefinition.ModuleSettingsOverrides == null)
			{
				sharepointDefinition.ModuleSettingsOverrides = new ModuleSettingsOverrides();
			}

			// Set instance naming definition on the DomDefinition level (takes priority over module level)
			sharepointDefinition.ModuleSettingsOverrides.NameDefinition = new DomInstanceNameDefinition
			{
				ConcatenationItems = new List<IDomInstanceConcatenationItem>
				{
					new FieldValueConcatenationItem { FieldDescriptorId = SharePointConfigurationDomMapper.SharePointConfigurationProperties.Name },
				},
			};

			Log($"Setting NameDefinition with {sharepointDefinition.ModuleSettingsOverrides.NameDefinition.ConcatenationItems.Count} concatenation items");

			Import(DomDefinitionExposers.Id.Equal(SharePointConfigurationDomMapper.DomDefinitionId.Id), sharepointDefinition);
		}
	}
}