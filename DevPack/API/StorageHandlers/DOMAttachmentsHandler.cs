namespace Skyline.DataMiner.Solutions.DocumentHub.API
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Paging;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.DTOs;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Exposers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Repositories.DomSource;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Validation;

	/// <summary>
	/// Storage handler for reading, paging, and uploading files from DOM instances.
	/// </summary>
	internal class DomAttachmentsHandler : IStorageHandler
	{
		private readonly IConnection _connection;
		private readonly IRepository<DomSource> _domSourceRepository;

		/// <summary>
		/// Initializes a new instance of the <see cref="DomAttachmentsHandler"/> class.
		/// </summary>
		/// <param name="connection">
		/// The active DataMiner connection to use for DOM operations.
		/// </param>
		public DomAttachmentsHandler(IConnection connection)
		{
			_connection = connection;
			_domSourceRepository = new DomSourceDomRepository(connection);
		}

		#region Public Methods

		/// <summary>
		/// Checks whether a specific file exists in a DOM instance.
		/// </summary>
		/// <param name="data">
		/// Data describing the DOM instance and file to check.
		/// </param>
		/// <returns>
		/// <c>true</c> if the file exists; otherwise <c>false</c>.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if the <paramref name="data"/> is not of type <see cref="DomFileExistsData"/>.
		/// </exception>
		public bool FileExists(FileExistsData data)
		{
			if (!(data is DomFileExistsData args))
				throw new ArgumentException("DOMAttachmentsHandler requires DOMFileExistsData.", nameof(data));

			var bucket = args.Bucket;
			if (bucket == null)
				throw new ArgumentException("Bucket must be specified.", nameof(data));

			var module = ResolveModule(bucket);

			// Create DOM helper for the module
			var domHelper = new DomHelper(_connection.HandleMessages, module);

			// Read the target DOM instance so we use its fully-qualified DomInstanceId
			// (carrying the correct ModuleId) instead of constructing one manually.
			var instance = domHelper.DomInstances
				.Read(DomInstanceExposers.Id.Equal(args.DomInstanceId))
				.FirstOrDefault();

			if (instance == null)
				return false;

			// Retrieve the file names for the given instance and check for match
			return domHelper.DomInstances.Attachments
				.GetFileNames(instance.ID)
				.Contains(args.Name, StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Reads all files based on the given read data.
		/// If <see cref="ReadData.Context"/> is set, reads only a single page.
		/// </summary>
		/// <param name="data">
		/// The data describing which files to read.
		/// </param>
		/// <returns>
		/// List of <see cref="IDocHubFile"/> representing the files found.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if the <paramref name="data"/> is not of type <see cref="DomFileReadData"/>.
		/// </exception>
		public List<IDocHubFile> ReadFiles(ReadData data)
		{
			if (!(data is DomFileReadData args))
				throw new ArgumentException("DOMAttachmentsHandler requires DOMFileReadData.", nameof(data));

			// If a paging context is already provided, read a single page
			if (args.Context != null)
				return ReadPage(data);

			// Create a fresh DOM page context for reading all files
			var context = new DOMPageData();
			args.Context = context;

			var files = new List<IDocHubFile>();

			// Keep reading pages until all data is exhausted
			while (args.Context.HasNextPage())
			{
				files.AddRange(ReadPage(data));
			}

			return files;
		}

		/// <summary>
		/// Uploads a file to a specific DOM instance.
		/// </summary>
		/// <param name="data">
		/// Data describing which file to upload and to which DOM instance.
		/// </param>
		/// <returns>
		/// The ID of the DOM instance as a string.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if <paramref name="data"/> is not of type <see cref="DomFileUploadData"/>.
		/// </exception>
		public string UploadFile(UploadData data)
		{
			if (!(data is DomFileUploadData args))
				throw new ArgumentException("DOMAttachmentsHandler requires DOMFileUploadData.", nameof(data));

			var filePath = args.FilePath;
			var newName = args.Name;
			var instanceId = args.DomInstanceId;

			// Load file into memory
			var fileBytes = File.ReadAllBytes(filePath);

			// Resolve the actual module name from the bucket's DOMSource reference
			var module = ResolveModule(args.Bucket);

			// Create DOM helper for the module
			var domHelper = new DomHelper(_connection.HandleMessages, module);

			// Read the target DOM instance
			var instance = domHelper.DomInstances
									.Read(DomInstanceExposers.Id.Equal(instanceId))
									.FirstOrDefault()
									?? throw new InvalidOperationException(
										$"Could not find DOM instance with id '{instanceId}' in module '{module}'.");

			// Add file as attachment to the DOM instance
			domHelper.DomInstances.Attachments.Add(instance.ID, newName, fileBytes);

			return instanceId.ToString();
		}

		/// <summary>
		/// Reads a single page of files based on the specified read data.
		/// Uses the <see cref="DOMPageData"/> context for paging state.
		/// </summary>
		/// <param name="data">
		/// The read data with optional DOMPageData context.
		/// </param>
		/// <returns>
		/// List of <see cref="IDocHubFile"/> representing a single page of files.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if <paramref name="data"/> is not <see cref="DomFileReadData"/> or if <paramref name="data.Context"/> is not <see cref="DOMPageData"/>.
		/// </exception>
		public List<IDocHubFile> ReadPage(ReadData data)
		{
			if (!(data is DomFileReadData args))
				throw new ArgumentException("DOMAttachmentsHandler requires DOMFileReadData.", nameof(data));

			if (!(args.Context is DOMPageData pagingHelper))
				throw new InvalidOperationException($"DOMAttachmentsHandler requires {nameof(DOMPageData)}.");

			int pageSize = args.Context.PageSize;
			var results = new List<IDocHubFile>();

			// Initialize modules if not already done
			InitializeModules(args, pagingHelper);

			// Exit immediately if there are no modules
			if (pagingHelper.Modules.Count == 0)
			{
				pagingHelper.Done = true;
				return results;
			}

			// Loop until we fill the page or all modules are processed
			while (results.Count < pageSize && pagingHelper.ModuleIndex < pagingHelper.Modules.Count)
			{
				string module = pagingHelper.Modules[pagingHelper.ModuleIndex];
				var domHelper = new DomHelper(_connection.HandleMessages, module);

				// Initialize paging helper if not yet initialized for this module
				if (pagingHelper.PagingHelper == null)
					pagingHelper.PagingHelper = domHelper.DomInstances.PreparePaging(BuildInstanceFilter(args));

				// Read all files from the current module
				ReadFromModule(domHelper, pagingHelper, args.Filter, results, pageSize);

				// Advance to next module if current module fully consumed
				if (pagingHelper.PagingHelper != null
					&& !pagingHelper.PagingHelper.HasNextPage()
					&& pagingHelper.InstanceIndexInPage >= pagingHelper.PagingHelper.GetCurrentPage().Count)
				{
					AdvanceToNextModule(pagingHelper);
				}
			}

			// Mark done if all modules have been processed
			if (pagingHelper.ModuleIndex >= pagingHelper.Modules.Count)
				pagingHelper.Done = true;

			return results.ToList();
		}
		#endregion

		#region Private Methods

		/// <summary>
		/// Initializes the module list in the page context.
		/// </summary>
		private void InitializeModules(DomFileReadData args, DOMPageData page)
		{
			if (page.Modules != null)
				return;

			// Use specific module or fetch all available modules
			page.Modules = !string.IsNullOrEmpty(args.Module)
				? new List<string> { args.Module }
				: GetAllSources(_domSourceRepository).ToList();

			// Mark done if no modules available
			if (page.Modules.Count == 0)
				page.Done = true;
		}

		/// <summary>
		/// Reads files from the current module, advancing paging helper and DOM instances.
		/// </summary>
		private void ReadFromModule(DomHelper domHelper, DOMPageData page, string filter, List<IDocHubFile> results, int pageSize)
		{
			while (results.Count < pageSize)
			{
				var instances = page.PagingHelper.GetCurrentPage()?.ToList();

				// Ensure we have a valid page with instances
				while (instances == null || page.InstanceIndexInPage >= instances.Count)
				{
					if (!page.PagingHelper.MoveToNextPage())
						return; // no more pages in this module

					page.InstanceIndexInPage = 0;
					instances = page.PagingHelper.GetCurrentPage()?.ToList();
				}

				var instance = instances[page.InstanceIndexInPage];
				var attachments = GetInstanceAttachments(domHelper, page, instance);

				// Apply filename filter
				if (!string.IsNullOrEmpty(filter))
				{
					attachments = attachments
						.Where(a => a.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
						.ToList();
				}

				// Add attachments
				while (page.AttachmentIndex < attachments.Count && results.Count < pageSize)
				{
					results.Add(new DomFileAdapter
					{
						Filename = attachments[page.AttachmentIndex++],
						Module = domHelper.ModuleId,
						Instance = instance,
					});
				}

				// If all attachments consumed → move to next instance
				if (page.AttachmentIndex >= attachments.Count)
				{
					page.AttachmentIndex = 0;
					page.CurrentAttachments = null;
					page.InstanceIndexInPage++;
				}
			}
		}

		/// <summary>
		/// Retrieves the attachment file names for a DOM instance.
		/// Reuses cached attachments if already loaded for the instance.
		/// </summary>
		private List<string> GetInstanceAttachments(DomHelper domHelper, DOMPageData page, DomInstance instance)
		{
			if (page.CurrentAttachments != null && page.CurrentInstanceId == instance.ID.Id)
				return page.CurrentAttachments;

			page.CurrentInstanceId = instance.ID.Id;
			page.CurrentAttachments = domHelper.DomInstances.Attachments
				.GetFileNames(instance.ID)
				.OrderBy(n => n)
				.ToList() ?? new List<string>();

			page.AttachmentIndex = 0;
			return page.CurrentAttachments;
		}

		/// <summary>
		/// Advances the paging context to the next module.
		/// </summary>
		private void AdvanceToNextModule(DOMPageData page)
		{
			page.PagingHelper = null;
			page.ModuleIndex++;
			page.InstanceIndexInPage = 0;
			page.AttachmentIndex = 0;
			page.CurrentInstanceId = Guid.Empty;
			page.CurrentAttachments = null;
		}

		/// <summary>
		/// Builds a DOM instance filter from bucket definition and/or instance IDs.
		/// Returns a TRUE filter when no filtering criteria are provided.
		/// </summary>
		private FilterElement<DomInstance> BuildInstanceFilter(DomFileReadData data)
		{
			FilterElement<DomInstance> filter = new TRUEFilterElement<DomInstance>();

			var definition = data?.Bucket?.Definition;
			if (!string.IsNullOrEmpty(definition))
			{
				filter = filter.AND(DomInstanceExposers.DomDefinitionId.Equal(Guid.Parse(definition)));
			}

			var ids = data?.DomInstanceIds;
			if (ids != null && ids.Count > 0)
			{
				FilterElement<DomInstance> idsFilter = new ORFilterElement<DomInstance>();

				foreach (var id in ids)
				{
					idsFilter = idsFilter.OR(DomInstanceExposers.Id.Equal(id));
				}

				filter = filter.AND(idsFilter);
			}

			return filter;
		}

		/// <summary>
		/// Retrieves all available DOM module names from the data helper.
		/// </summary>
		private IEnumerable<string> GetAllSources(IRepository<DomSource> helper)
		{
			return helper.Read(new TRUEFilterElement<DomSource>()).Select(s => s.Module);
		}

		/// <summary>
		/// Resolves the actual DOM module name from the bucket's DOMSource reference.
		/// </summary>
		/// <param name="bucket">The document bucket containing the DOMSource reference.</param>
		/// <returns>The module name string.</returns>
		/// <exception cref="ArgumentException">
		/// Thrown if the bucket's DOMSource reference is invalid or the DomSource cannot be found.
		/// </exception>
		private string ResolveModule(DocumentBucket bucket)
		{
			if (!bucket.DOMSource.IsValidReference(out Guid domSourceId))
			{
				throw new ArgumentException(
					$"The bucket '{bucket.Name}' does not have a valid DOM Source reference.");
			}

			var domSource = _domSourceRepository
				.Read(DomSourceExposers.Identifier.Equal(domSourceId.ToString()))
				.FirstOrDefault();

			if (domSource == null)
			{
				throw new ArgumentException(
					$"The DOM Source (ID: {domSourceId}) tied to the bucket '{bucket.Name}' does not exist in the repository.");
			}

			if (string.IsNullOrEmpty(domSource.Module))
			{
				throw new ArgumentException(
					$"The DOM Source '{domSource.Name}' does not have a module configured.");
			}

			return domSource.Module;
		}
		#endregion
	}
}