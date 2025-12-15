using System;
using System.Collections.Generic;
using System.Text;
using static DomHelpers.SlcDocumenthub.SlcDocumenthubIds.Enums;

namespace Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub
{
    public static class Models
    {
        public class DocumentCategory
        {
            public Guid ID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string UploadPath { get; set; }
            public Storagetype StorageType { get; set; }
            public string Extensions { get; set; }
            public bool IsDefault { get; set; }
        }

        public class SharePointConfiguration
        {
            public Guid ID { get; set; }
            public string TenantID { get; set; }
            public string ClientID { get; set; }
            public string ClientSecret { get; set; }
            public string SiteURL { get; set; }
            public string DocumentLibraryName { get; set; }
        }
    }
}
