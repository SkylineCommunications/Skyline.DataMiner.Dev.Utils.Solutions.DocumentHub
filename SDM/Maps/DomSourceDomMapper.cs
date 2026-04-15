namespace Skyline.DataMiner.DocumentHub.SDM.Models
{
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Sections;
    using Skyline.DataMiner.SDM;
    using System;

    [SdmDomMapper]
    internal static class DomSourceDomMapper
    {
        internal const string ModuleId = "(slc)documenthub";
        public static DomDefinitionId DomDefinitionId = new DomDefinitionId(new Guid("5ec0807d-7e0f-4fc2-9e60-53db664614f7"))
        { ModuleId = ModuleId };
        public static class DomSourceProperties
        {
            public static SectionDefinitionID SectionDefinitionId = new SectionDefinitionID(new Guid("a98e6176-ce32-45d1-a8f8-e1baea6d4488"))
            { ModuleId = ModuleId };
            public static FieldDescriptorID Name = new FieldDescriptorID(new Guid("fdf73d52-590c-4b67-8d92-a367651790eb"));
            public static FieldDescriptorID Module = new FieldDescriptorID(new Guid("98bdd53d-deda-497a-8394-2f503d294015"));
            public static FieldDescriptorID NetworkSharePath = new FieldDescriptorID(new Guid("2ccfdd77-f0a5-4607-8a52-afba8c3bef82"));
            public static FieldDescriptorID Credential = new FieldDescriptorID(new Guid("ec214b80-298c-421f-b352-c2a3d5200de9"));
        }
    }
}
