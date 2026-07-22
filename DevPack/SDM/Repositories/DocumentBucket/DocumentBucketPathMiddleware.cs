namespace Skyline.DataMiner.Solutions.DocumentHub.SDM.Repositories.DocumentBucket
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.SDM;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

    /// <summary>
    /// Middleware that sanitizes the <see cref="DocumentBucket.UploadPath"/> on create and update operations.
    /// Ensures that paths are stored without leading slashes so that handlers can resolve them consistently.
    /// </summary>
    internal sealed class DocumentBucketPathMiddleware :
        IMiddlewareMarker<DocumentBucket>,
        ICreatableMiddleware<DocumentBucket>,
        IBulkCreatableMiddleware<DocumentBucket>,
        IUpdatableMiddleware<DocumentBucket>,
        IBulkUpdatableMiddleware<DocumentBucket>
    {
        /// <inheritdoc/>
        public DocumentBucket OnCreate(DocumentBucket oToCreate, Func<DocumentBucket, DocumentBucket> next)
        {
            SanitizePath(oToCreate);
            return next(oToCreate);
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<DocumentBucket> OnCreate(IEnumerable<DocumentBucket> oToCreate, Func<IEnumerable<DocumentBucket>, IReadOnlyCollection<DocumentBucket>> next)
        {
            var items = oToCreate.ToList();
            foreach (var item in items)
            {
                SanitizePath(item);
            }

            return next(items);
        }

        /// <inheritdoc/>
        public DocumentBucket OnUpdate(DocumentBucket oToUpdate, Func<DocumentBucket, DocumentBucket> next)
        {
            SanitizePath(oToUpdate);
            return next(oToUpdate);
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<DocumentBucket> OnUpdate(IEnumerable<DocumentBucket> oToUpdate, Func<IEnumerable<DocumentBucket>, IReadOnlyCollection<DocumentBucket>> next)
        {
            var items = oToUpdate.ToList();
            foreach (var item in items)
            {
                SanitizePath(item);
            }

            return next(items);
        }

        private static void SanitizePath(DocumentBucket bucket)
        {
            if (bucket == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(bucket.UploadPath))
            {
                bucket.UploadPath = bucket.UploadPath.TrimStart('/', '\\');
            }
        }
    }
}
