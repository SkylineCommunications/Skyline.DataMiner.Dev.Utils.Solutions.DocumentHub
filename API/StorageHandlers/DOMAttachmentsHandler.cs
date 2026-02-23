namespace Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers;
    using Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers.FileAdapters;
    using Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers.Paging;

    /// <summary>
    /// Storage handler for reading, paging, and uploading files from DOM instances.
    /// </summary>
    internal class DOMAttachmentsHandler : IStorageHandler
	{
		private readonly IConnection _connection;
		private readonly DataHelperDOMSource _dataHelperDomSource;

		/// <summary>
		/// Initializes a new instance of the <see cref="DOMAttachmentsHandler"/> class.
		/// </summary>
		/// <param name="connection">
		/// The active DataMiner connection to use for DOM operations.
		/// </param>
		public DOMAttachmentsHandler(IConnection connection)
		{
			_connection = connection;
			_dataHelperDomSource = new DataHelperDOMSource(connection);
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
		/// Thrown if the <paramref name="data"/> is not of type <see cref="DOMFileExistsData"/>.
		/// </exception>
		public bool FileExists(FileExistsData data)
		{
			if (!(data is DOMFileExistsData args))
				throw new ArgumentException("DOMAttachmentsHandler requires DOMFileExistsData.", nameof(data));

			var category = args.Category;
			if (category == null || string.IsNullOrEmpty(category.DOMSource.Module))
				throw new ArgumentException("Category and its module must be specified.", nameof(data));

			// Create DOM helper for the module
			var domHelper = new DomHelper(_connection.HandleMessages, category.DOMSource.Module);

			// Retrieve the file names for the given instance and check for match
			return domHelper.DomInstances.Attachments
				.GetFileNames(new DomInstanceId(args.DomInstanceId))
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
		/// Thrown if the <paramref name="data"/> is not of type <see cref="DOMFileReadData"/>.
		/// </exception>
		public List<IDocHubFile> ReadFiles(ReadData data)
		{
			if (!(data is DOMFileReadData args))
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
		/// Thrown if <paramref name="data"/> is not of type <see cref="DOMFileUploadData"/>.
		/// </exception>
		public string UploadFile(UploadData data)
		{
			if (!(data is DOMFileUploadData args))
				throw new ArgumentException("DOMAttachmentsHandler requires DOMFileUploadData.", nameof(data));

			var filePath = args.FilePath;
			var newName = args.Name;
			var instanceId = args.DomInstanceId;

			// Load file into memory
			var fileBytes = File.ReadAllBytes(filePath);

			// Add file as attachment to the DOM instance
			var domHelper = new DomHelper(_connection.HandleMessages, args.Category.DOMSource.Module);
			domHelper.DomInstances.Attachments.Add(new DomInstanceId(instanceId), newName, fileBytes);

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
		/// Thrown if <paramref name="data"/> is not <see cref="DOMFileReadData"/> or if <paramref name="data.Context"/> is not <see cref="DOMPageData"/>.
		/// </exception>
		public List<IDocHubFile> ReadPage(ReadData data)
		{
			if (!(data is DOMFileReadData args))
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

			return results.Cast<IDocHubFile>().ToList();
		}
		#endregion

		#region Private Methods

		/// <summary>
		/// Initializes the module list in the page context.
		/// </summary>
		private void InitializeModules(DOMFileReadData args, DOMPageData page)
		{
			if (page.Modules != null)
				return;

			// Use specific module or fetch all available modules
			page.Modules = !string.IsNullOrEmpty(args.Module)
				? new List<string> { args.Module }
				: GetAllSources(_dataHelperDomSource).ToList();

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
				// Get current DOM page
				var instances = page.PagingHelper.GetCurrentPage()?.ToList();

				// If no more instances in page, move to next DOM page
				if (instances == null || page.InstanceIndexInPage >= instances.Count)
				{
					if (!page.PagingHelper.MoveToNextPage())
						break; // no more pages

					instances = page.PagingHelper.GetCurrentPage().ToList();
					page.InstanceIndexInPage = 0;
				}

				// Process current instance
				var instance = instances[page.InstanceIndexInPage];
				var attachments = GetInstanceAttachments(domHelper, page, instance);

                // Apply filename filter (case-insensitive substring match)
                if (!string.IsNullOrEmpty(filter))
                {
                    attachments = attachments
                        .Where(a => a.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();
                }

                // Add attachments to results until page is full
                while (page.AttachmentIndex < attachments.Count && results.Count < pageSize)
				{
					results.Add(new DomFileAdapter
					{
						Filename = attachments[page.AttachmentIndex],
						Module = domHelper.ModuleId,
						Instance = instance,
					});
					page.AttachmentIndex++;
				}

				// Move to next instance if all attachments processed
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
		/// Builds a DOM instance filter from category definition and/or instance IDs.
		/// Returns a TRUE filter when no filtering criteria are provided.
		/// </summary>
		private FilterElement<DomInstance> BuildInstanceFilter(DOMFileReadData data)
		{
			FilterElement<DomInstance> filter = new TRUEFilterElement<DomInstance>();

			var definition = data?.Category?.Definition;
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
		private IEnumerable<string> GetAllSources(DataHelperDOMSource helper)
		{
			return helper.Read().Select(s => s.Module);
		}
		#endregion
	}
}
