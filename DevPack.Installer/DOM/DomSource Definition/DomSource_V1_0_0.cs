namespace DevPack.Installer.DOM.DomSource_Definition
{
	using System;
	using System.Collections.Generic;
	using DevPack.Installer.DOM;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Utils.DOM.Builders;

	internal class DomSource_V1_0_0 : DomMigration
	{
		public DomSource_V1_0_0(IConnection connection, Action<string> logMethod = null) : base(connection, DomSourceDomMapper.ModuleId, logMethod)
		{
		}

		public override void Migrate()
		{
			var section = new SectionDefinitionBuilder()
				.WithID(DomSourceDomMapper.DomSourceProperties.SectionDefinitionId)
				.WithName(nameof(DomSourceDomMapper.DomSourceProperties))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DomSourceDomMapper.DomSourceProperties.Name)
					.WithName(nameof(DomSourceDomMapper.DomSourceProperties.Name))
					.WithType(typeof(string))
					.WithIsOptional(false)
					.WithTooltip("Human readable name for the DOM source"))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DomSourceDomMapper.DomSourceProperties.Module)
					.WithName(nameof(DomSourceDomMapper.DomSourceProperties.Module))
					.WithType(typeof(string))
					.WithIsOptional(false)
					.WithTooltip("The DOM module identifier where documents are stored"))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DomSourceDomMapper.DomSourceProperties.NetworkSharePath)
					.WithName(nameof(DomSourceDomMapper.DomSourceProperties.NetworkSharePath))
					.WithType(typeof(string))
					.WithIsOptional(false)
					.WithTooltip("Optional network share path for document storage (e.g., \\\\server\\share\\folder)"))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DomSourceDomMapper.DomSourceProperties.Credential)
					.WithType(typeof(string))
					.WithName(nameof(DomSourceDomMapper.DomSourceProperties.Credential))
					.WithIsOptional(false)
					.WithTooltip("Credential reference for accessing the network share or external storage"))
				.Build();

			Import(SectionDefinitionExposers.ID.Equal(DomSourceDomMapper.DomSourceProperties.SectionDefinitionId), section);

			var domSourceDefinition = new DomDefinitionBuilder()
				.WithID(DomSourceDomMapper.DomDefinitionId)
				.WithName("DOMSource")
				.AddSectionDefinitionLink(new Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions.SectionDefinitionLink
				{
					SectionDefinitionID = DomSourceDomMapper.DomSourceProperties.SectionDefinitionId,
					AllowMultipleSections = false,
					IsOptional = false,
					IsSoftDeleted = false,
				})
				.Build();

			// Ensure ModuleSettingsOverrides is not null before setting NameDefinition
			if (domSourceDefinition.ModuleSettingsOverrides == null)
			{
				domSourceDefinition.ModuleSettingsOverrides = new ModuleSettingsOverrides();
			}

			// Set instance naming definition on the DomDefinition level (takes priority over module level)
			domSourceDefinition.ModuleSettingsOverrides.NameDefinition = new DomInstanceNameDefinition
			{
				ConcatenationItems = new List<IDomInstanceConcatenationItem>
				{
					new FieldValueConcatenationItem { FieldDescriptorId = DomSourceDomMapper.DomSourceProperties.Name },
				},
			};

			Log($"Setting NameDefinition with {domSourceDefinition.ModuleSettingsOverrides.NameDefinition.ConcatenationItems.Count} concatenation items");

			Import(DomDefinitionExposers.Id.Equal(DomSourceDomMapper.DomDefinitionId.Id), domSourceDefinition);
		}
	}
}