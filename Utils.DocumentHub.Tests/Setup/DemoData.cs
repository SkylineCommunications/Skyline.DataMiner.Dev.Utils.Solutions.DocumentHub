namespace Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup
{
	using System.Collections.Generic;

	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	public static class DemoData
	{
		public static List<DomSource> DomSources => new List<DomSource>
		{
			new DomSource
			{
				Name = "Inventory Attachments",
				Module = "inventory",
				NetworkSharePath = @"\\server01\inventory\attachments",
				Credential = "cred-inventory-001",
			},
			new DomSource
			{
				Name = "Maintenance Logs",
				Module = "maintenance",
				NetworkSharePath = @"\\server01\maintenance\logs",
				Credential = "cred-maintenance-001",
			},
			new DomSource
			{
				Name = "Network Diagrams",
				Module = "network",
				NetworkSharePath = @"\\server02\network\diagrams",
				Credential = "cred-network-001",
			},
			new DomSource
			{
				Name = "Service Reports",
				Module = "inventory",
				NetworkSharePath = @"\\server02\services\reports",
				Credential = "cred-inventory-001",
			},
			new DomSource
			{
				Name = "Audit Trail",
				Module = "audit",
				NetworkSharePath = @"\\server03\audit\trail",
				Credential = "cred-audit-001",
			},
		};

		public static List<DocumentCategory> DocumentCategories => new List<DocumentCategory>
		{
			new DocumentCategory
			{
				Name = "Technical Documentation",
				Description = "Technical manuals and guides",
				UploadPath = "/docs/technical",
				StorageType = StorageType.SharePoint,
				Extensions = "pdf,docx",
				IsDefault = true,
				Definition = "TechDocs",
				SizeLimit = 10240,
			},
			new DocumentCategory
			{
				Name = "Network Diagrams",
				Description = "Network topology and diagrams",
				UploadPath = "/docs/network",
				StorageType = StorageType.DOM,
				Extensions = "png,vsdx,pdf",
				IsDefault = false,
				Definition = "NetDiag",
				SizeLimit = 20480,
			},
			new DocumentCategory
			{
				Name = "Maintenance Reports",
				Description = "Scheduled maintenance reports",
				UploadPath = "/docs/maintenance",
				StorageType = StorageType.SharePoint,
				Extensions = "pdf,xlsx",
				IsDefault = false,
				Definition = "MaintRep",
				SizeLimit = 10240,
			},
			new DocumentCategory
			{
				Name = "Configuration Backups",
				Description = "Device configuration backups",
				UploadPath = "/docs/config",
				StorageType = StorageType.Local,
				Extensions = "xml,json,cfg",
				IsDefault = false,
				Definition = "ConfigBak",
				SizeLimit = 5120,
			},
			new DocumentCategory
			{
				Name = "Service Level Agreements",
				Description = "SLA documents and contracts",
				UploadPath = "/docs/sla",
				StorageType = StorageType.SharePoint,
				Extensions = "pdf,docx",
				IsDefault = false,
				Definition = "SLA",
				SizeLimit = 10240,
			},
		};

		public static List<SharePointConfiguration> SharePointConfigurations => new List<SharePointConfiguration>
		{
			new SharePointConfiguration
			{
				TenantID = "tenant-aaa-111",
				ClientID = "client-aaa-111",
				ClientSecret = "secret-aaa-111",
				SiteURL = "https://contoso.sharepoint.com/sites/Engineering",
				DocumentLibraryName = "Shared Documents",
				Status = SharePointConfigurationStatus.Connected,
			},
			new SharePointConfiguration
			{
				TenantID = "tenant-bbb-222",
				ClientID = "client-bbb-222",
				ClientSecret = "secret-bbb-222",
				SiteURL = "https://contoso.sharepoint.com/sites/Engineering",
				DocumentLibraryName = "Technical Manuals",
				Status = SharePointConfigurationStatus.Connected,
			},
			new SharePointConfiguration
			{
				TenantID = "tenant-ccc-333",
				ClientID = "client-ccc-333",
				ClientSecret = "secret-ccc-333",
				SiteURL = "https://fabrikam.sharepoint.com/sites/Operations",
				DocumentLibraryName = "Shared Documents",
				Status = SharePointConfigurationStatus.Disconnected,
			},
			new SharePointConfiguration
			{
				TenantID = "tenant-ddd-444",
				ClientID = "client-aaa-111",
				ClientSecret = "secret-ddd-444",
				SiteURL = "https://fabrikam.sharepoint.com/sites/Sales",
				DocumentLibraryName = "Proposals",
				Status = SharePointConfigurationStatus.Connected,
			},
			new SharePointConfiguration
			{
				TenantID = "tenant-eee-555",
				ClientID = "client-eee-555",
				ClientSecret = "secret-eee-555",
				SiteURL = "https://contoso.sharepoint.com/sites/Engineering",
				DocumentLibraryName = "Design Documents",
				Status = SharePointConfigurationStatus.Disconnected,
			},
		};
	}
}