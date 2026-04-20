namespace Skyline.DataMiner.Solutions.DocumentHub.SDM.Maps
{
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Sections;
    using Skyline.DataMiner.SDM;
    using System;

    [SdmDomMapper]
    internal static class SharePointConfigurationDomMapper
    {
        internal const string ModuleId = "(slc)documenthub";
        public static DomDefinitionId DomDefinitionId = new DomDefinitionId(new Guid("f0492169-2e70-4aa3-a06a-28e8091b785a"))
        { ModuleId = ModuleId };

        internal static class ConfigurationProperties
        {
            internal static SectionDefinitionID SectionDefinitionId = new SectionDefinitionID(new Guid("2b038372-b61c-43d4-a39d-325e058a62fc"))
            { ModuleId = ModuleId };
            internal static FieldDescriptorID ClientID = new FieldDescriptorID(new Guid("2b9b4f74-7863-49cc-bef8-7bbc102d9f64"));
            internal static FieldDescriptorID SiteURL = new FieldDescriptorID(new Guid("b4da0186-d6a7-4958-b949-ea5776e9e1c6"));
            internal static FieldDescriptorID ClientSecret = new FieldDescriptorID(new Guid("c41be706-8539-46ab-b886-14e89db9403e"));
            internal static FieldDescriptorID DocumentLibraryName = new FieldDescriptorID(new Guid("d335800d-f136-4fae-a9ea-0df843394d89"));
            internal static FieldDescriptorID Status = new FieldDescriptorID(new Guid("e1d9468d-69b7-4b51-856d-70c30ffb74a0"));
            internal static FieldDescriptorID TenantID = new FieldDescriptorID(new Guid("f14e860b-7d10-46db-aae9-82817d2e2862"));
        }
    }
}
