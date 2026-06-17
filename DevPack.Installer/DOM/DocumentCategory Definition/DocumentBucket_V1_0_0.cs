namespace Skyline.DataMiner.Solutions.DocumentHub.Installer.DOM.DocumentBucket_Definition
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

	internal class DocumentBucket_V1_0_0 : DomMigration
	{
		public DocumentBucket_V1_0_0(IConnection connection, Action<string> logMethod = null) : base(connection, DocumentBucketDomMapper.ModuleId, logMethod)
		{
		}

		public override void Migrate()
		{
			var section = new SectionDefinitionBuilder()
				.WithID(DocumentBucketDomMapper.DocumentBucketProperties.SectionDefinitionId)
				.WithName(nameof(DocumentBucketDomMapper.DocumentBucketProperties))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DocumentBucketDomMapper.DocumentBucketProperties.Name)
					.WithName(nameof(DocumentBucketDomMapper.DocumentBucketProperties.Name))
					.WithType(typeof(string))
					.WithIsOptional(false)
					.WithTooltip("Human readable name of the bucket"))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DocumentBucketDomMapper.DocumentBucketProperties.UploadPath)
					.WithName(nameof(DocumentBucketDomMapper.DocumentBucketProperties.UploadPath))
					.WithType(typeof(string))
					.WithIsOptional(false)
					.WithTooltip("The path where uploaded documents will be stored"))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DocumentBucketDomMapper.DocumentBucketProperties.Description)
					.WithName(nameof(DocumentBucketDomMapper.DocumentBucketProperties.Description))
					.WithType(typeof(string))
					.WithIsOptional(true)
					.WithTooltip("A detailed description of the bucket"))
				.AddFieldDescriptor(new GenericEnumFieldDescriptorBuilder()
					.WithID(DocumentBucketDomMapper.DocumentBucketProperties.StorageType)
					.WithName(nameof(DocumentBucketDomMapper.DocumentBucketProperties.StorageType))
					.WithEnumType(GenericEnumFieldDescriptorBuilder.EnumType.Int)
					.AddEnumValue(new GenericEnumEntry<int>("Local", 0))
					.AddEnumValue(new GenericEnumEntry<int>("Sharepoint", 1))
					.AddEnumValue(new GenericEnumEntry<int>("DOM", 2))
					.WithIsOptional(false)
					.WithTooltip("The storage where the file needs to be saved to."))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DocumentBucketDomMapper.DocumentBucketProperties.Extensions)
					.WithType(typeof(string))
					.WithName(nameof(DocumentBucketDomMapper.DocumentBucketProperties.Extensions))
					.WithIsOptional(false)
					.WithTooltip("Comma-separated list of allowed file extensions (e.g., .pdf,.docx,.txt)"))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DocumentBucketDomMapper.DocumentBucketProperties.IsDefault)
					.WithType(typeof(bool))
					.WithName(nameof(DocumentBucketDomMapper.DocumentBucketProperties.IsDefault))
					.WithIsOptional(false)
					.WithTooltip("Indicates whether this is the default bucket for document uploads"))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DocumentBucketDomMapper.DocumentBucketProperties.Definition)
					.WithType(typeof(string))
					.WithName(nameof(DocumentBucketDomMapper.DocumentBucketProperties.Definition))
					.WithIsOptional(true)
					.WithTooltip("Additional definition or configuration data for the bucket"))
				.AddFieldDescriptor(new DomInstanceFieldDescriptorBuilder()
					.WithID(DocumentBucketDomMapper.DocumentBucketProperties.DOMSource)
					.WithName(nameof(DocumentBucketDomMapper.DocumentBucketProperties.DOMSource))
					.WithType(typeof(Guid))
					.WithIsOptional(true)
					.WithModule(DomSourceDomMapper.ModuleId)
					.WithDefinitions(new[] { DomSourceDomMapper.DomDefinitionId })
					.WithTooltip("Reference to the DOM source configuration for this bucket"))
				.AddFieldDescriptor(new DomInstanceFieldDescriptorBuilder()
					.WithID(DocumentBucketDomMapper.DocumentBucketProperties.SharePointConfiguration)
					.WithName(nameof(DocumentBucketDomMapper.DocumentBucketProperties.SharePointConfiguration))
					.WithType(typeof(Guid))
					.WithIsOptional(true)
					.WithModule(SharePointConfigurationDomMapper.ModuleId)
					.WithDefinitions(new[] { SharePointConfigurationDomMapper.DomDefinitionId })
					.WithTooltip("Reference to the SharePoint configuration for this bucket"))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DocumentBucketDomMapper.DocumentBucketProperties.SizeLimit)
					.WithName(nameof(DocumentBucketDomMapper.DocumentBucketProperties.SizeLimit))
					.WithType(typeof(long))
					.WithIsOptional(true)
					.WithTooltip("Maximum file size allowed for uploads in this bucket (in bytes). Leave empty for no limit."))
				.Build();

			Import(SectionDefinitionExposers.ID.Equal(DocumentBucketDomMapper.DocumentBucketProperties.SectionDefinitionId), section);

			var documentbucketDefinition = new DomDefinitionBuilder()
				.WithID(DocumentBucketDomMapper.DomDefinitionId)
				.WithName("Document bucket")
				.AddSectionDefinitionLink(new Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions.SectionDefinitionLink
				{
					SectionDefinitionID = DocumentBucketDomMapper.DocumentBucketProperties.SectionDefinitionId,
					AllowMultipleSections = false,
					IsOptional = false,
					IsSoftDeleted = false,
				})
				.Build();

			// Ensure ModuleSettingsOverrides is not null before setting NameDefinition
			if (documentbucketDefinition.ModuleSettingsOverrides == null)
			{
				documentbucketDefinition.ModuleSettingsOverrides = new ModuleSettingsOverrides();
			}

			// Set instance naming definition on the DomDefinition level (takes priority over module level)
			documentbucketDefinition.ModuleSettingsOverrides.NameDefinition = new DomInstanceNameDefinition
			{
				ConcatenationItems = new List<IDomInstanceConcatenationItem>
				{
					new FieldValueConcatenationItem { FieldDescriptorId = DocumentBucketDomMapper.DocumentBucketProperties.Name },
				},
			};

			Log($"Setting NameDefinition with {documentbucketDefinition.ModuleSettingsOverrides.NameDefinition.ConcatenationItems.Count} concatenation items");

			Import(DomDefinitionExposers.Id.Equal(DocumentBucketDomMapper.DomDefinitionId.Id), documentbucketDefinition);
		}
	}
}