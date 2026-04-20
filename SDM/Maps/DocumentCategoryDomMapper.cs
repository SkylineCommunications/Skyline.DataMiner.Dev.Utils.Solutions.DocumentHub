namespace Skyline.DataMiner.Solutions.DocumentHub.SDM.Maps
{
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Sections;
    using Skyline.DataMiner.SDM;
    using System;

    [SdmDomMapper]
    internal class DocumentCategoryDomMapper
    {
        internal const string ModuleId = "(slc)documenthub";
        internal static DomDefinitionId DomDefinitionId = new DomDefinitionId(new Guid("c5a9829c-5ca1-4a29-87d0-88e4e4d13ace"))
        { ModuleId = ModuleId };

        internal static class DocumentCategoryProperties
        {
            internal static SectionDefinitionID SectionDefinitionId = new SectionDefinitionID(new Guid("9dee7951-0ce8-42d1-8601-f1eb8c28a12c"))
            { ModuleId = ModuleId };
            internal static FieldDescriptorID Name = new FieldDescriptorID(new Guid("d86b22d3-b231-4bc0-8c70-d38c50fd10d8"));
            internal static FieldDescriptorID UploadPath = new FieldDescriptorID(new Guid("a45f2ac8-a6e4-4650-afe2-a6ea7f4262b8"));
            internal static FieldDescriptorID Description = new FieldDescriptorID(new Guid("dc4c9953-9bdf-421f-9a0f-384ca4eace07"));
            internal static FieldDescriptorID StorageType = new FieldDescriptorID(new Guid("36c92e1d-0108-4e6d-a411-189ecd9869ea"));
            internal static FieldDescriptorID Extensions = new FieldDescriptorID(new Guid("9a62750a-de42-4241-b937-54ea4b79ca90"));
            internal static FieldDescriptorID IsDefault = new FieldDescriptorID(new Guid("f0b2c67c-34f5-4aea-b869-68c134a7bdd8"));
            internal static FieldDescriptorID Definition = new FieldDescriptorID(new Guid("239b1596-a92c-4552-8086-701270068fbf"));
            internal static FieldDescriptorID DOMSource = new FieldDescriptorID(new Guid("375d860c-9b5d-424b-a1bf-88ceb3a9a280"));
        }
    }
}
