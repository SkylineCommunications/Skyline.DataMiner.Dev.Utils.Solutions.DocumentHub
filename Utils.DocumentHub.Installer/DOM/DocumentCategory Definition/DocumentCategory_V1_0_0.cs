namespace Skyline.DataMiner.Utils.DocumentHub.Installer.DOM.DocumentCategory_Definition
{
	using System;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.GenericEnums;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Utils.DocumentHub.Installer.DOM;
	using Skyline.DataMiner.Utils.DOM.Builders;

	internal class DocumentCategory_V1_0_0 : DomMigration
	{
		public DocumentCategory_V1_0_0(IConnection connection, Action<string> logMethod = null) : base(connection, DocumentCategoryDomMapper.ModuleId, logMethod)
		{
		}

		public override void Migrate()
		{
			var section = new SectionDefinitionBuilder()
				.WithID(DocumentCategoryDomMapper.DocumentCategoryProperties.SectionDefinitionId)
				.WithName(nameof(DocumentCategoryDomMapper.DocumentCategoryProperties))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DocumentCategoryDomMapper.DocumentCategoryProperties.Name)
					.WithName(nameof(DocumentCategoryDomMapper.DocumentCategoryProperties.Name))
					.WithType(typeof(string))
					.WithIsOptional(false)
					.WithTooltip("Human readable name of the category"))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DocumentCategoryDomMapper.DocumentCategoryProperties.UploadPath)
					.WithName(nameof(DocumentCategoryDomMapper.DocumentCategoryProperties.UploadPath))
					.WithType(typeof(string))
					.WithIsOptional(false)
					.WithTooltip("The path where uploaded documents will be stored"))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DocumentCategoryDomMapper.DocumentCategoryProperties.Description)
					.WithName(nameof(DocumentCategoryDomMapper.DocumentCategoryProperties.Description))
					.WithType(typeof(string))
					.WithIsOptional(true)
					.WithTooltip("A detailed description of the category"))
				.AddFieldDescriptor(new GenericEnumFieldDescriptorBuilder()
					.WithID(DocumentCategoryDomMapper.DocumentCategoryProperties.StorageType)
					.WithName(nameof(DocumentCategoryDomMapper.DocumentCategoryProperties.StorageType))
					.WithEnumType(GenericEnumFieldDescriptorBuilder.EnumType.Int)
					.AddEnumValue(new GenericEnumEntry<int>("Local", 0))
					.AddEnumValue(new GenericEnumEntry<int>("Sharepoint", 1))
					.AddEnumValue(new GenericEnumEntry<int>("DOM", 2))
					.WithIsOptional(false)
					.WithTooltip("The storage where the file needs to be saved to."))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DocumentCategoryDomMapper.DocumentCategoryProperties.Extensions)
					.WithType(typeof(string))
					.WithName(nameof(DocumentCategoryDomMapper.DocumentCategoryProperties.Extensions))
					.WithIsOptional(false)
					.WithTooltip("Comma-separated list of allowed file extensions (e.g., .pdf,.docx,.txt)"))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DocumentCategoryDomMapper.DocumentCategoryProperties.IsDefault)
					.WithType(typeof(bool))
					.WithName(nameof(DocumentCategoryDomMapper.DocumentCategoryProperties.IsDefault))
					.WithIsOptional(false)
					.WithTooltip("Indicates whether this is the default category for document uploads"))
				.AddFieldDescriptor(new FieldDescriptorBuilder()
					.WithID(DocumentCategoryDomMapper.DocumentCategoryProperties.Definition)
					.WithType(typeof(string))
					.WithName(nameof(DocumentCategoryDomMapper.DocumentCategoryProperties.Definition))
					.WithIsOptional(true)
					.WithTooltip("Additional definition or configuration data for the category"))
				.AddFieldDescriptor(new DomInstanceFieldDescriptorBuilder()
					.WithID(DocumentCategoryDomMapper.DocumentCategoryProperties.DOMSource)
					.WithName(nameof(DocumentCategoryDomMapper.DocumentCategoryProperties.DOMSource))
					.WithType(typeof(Guid))
					.WithIsOptional(true)
					.WithModule(DomSourceDomMapper.ModuleId)
					.WithDefinitions(new[] { DomSourceDomMapper.DomDefinitionId })
					.WithTooltip("Reference to the DOM source configuration for this category"))
				.Build();

			Import(SectionDefinitionExposers.ID.Equal(DocumentCategoryDomMapper.DocumentCategoryProperties.SectionDefinitionId), section);

			var documentCategoryDefinition = new DomDefinitionBuilder()
				.WithID(DocumentCategoryDomMapper.DomDefinitionId)
				.WithName("Document Category")
				.AddSectionDefinitionLink(new Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions.SectionDefinitionLink
				{
					SectionDefinitionID = DocumentCategoryDomMapper.DocumentCategoryProperties.SectionDefinitionId,
					AllowMultipleSections = false,
					IsOptional = false,
					IsSoftDeleted = false,
				})
				.Build();

			Import(DomDefinitionExposers.Id.Equal(DocumentCategoryDomMapper.DomDefinitionId.Id), documentCategoryDefinition);
		}
	}
}