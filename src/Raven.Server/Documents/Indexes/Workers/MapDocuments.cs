﻿using System;
using System.Diagnostics;
using System.Threading;
using Raven.Abstractions.Logging;
using Raven.Server.Config.Categories;
using Raven.Server.Documents.Indexes.Persistence.Lucene;
using Raven.Server.ServerWide.Context;

namespace Raven.Server.Documents.Indexes.Workers
{
    public class MapDocuments : IIndexingWork
    {
        protected readonly ILog Log = LogManager.GetLogger(typeof(CleanupDeletedDocuments));

        private readonly Index _index;
        private readonly IndexingConfiguration _configuration;
        private readonly DocumentsStorage _documentsStorage;
        private readonly IndexStorage _indexStorage;

        public MapDocuments(Index index, DocumentsStorage documentsStorage, IndexStorage indexStorage, IndexingConfiguration configuration)
        {
            _index = index;
            _configuration = configuration;
            _documentsStorage = documentsStorage;
            _indexStorage = indexStorage;
        }

        public bool Execute(DocumentsOperationContext databaseContext, TransactionOperationContext indexContext,
            Lazy<IndexWriteOperation> writeOperation, IndexingBatchStats stats, CancellationToken token)
        {
            var pageSize = _configuration.MaxNumberOfTombstonesToFetch;
            var timeoutProcessing = Debugger.IsAttached == false ? _configuration.DocumentProcessingTimeout.AsTimeSpan : TimeSpan.FromMinutes(15);

            var moreWorkFound = false;

            foreach (var collection in _index.Collections)
            {
                if (Log.IsDebugEnabled)
                    Log.Debug($"Executing map for '{_index.Name} ({_index.IndexId})'. Collection: {collection}.");
                
                var lastMappedEtag = _indexStorage.ReadLastMappedEtag(indexContext.Transaction, collection);

                if (Log.IsDebugEnabled)
                    Log.Debug($"Executing map for '{_index.Name} ({_index.IndexId})'. LastMappedEtag: {lastMappedEtag}.");

                var lastEtag = lastMappedEtag;
                var count = 0;

                var sw = Stopwatch.StartNew();
                IndexWriteOperation indexWriter = null;

                using (databaseContext.OpenReadTransaction())
                {
                    foreach (var document in _documentsStorage.GetDocumentsAfter(databaseContext, collection, lastEtag + 1, 0, pageSize))
                    {
                        token.ThrowIfCancellationRequested();

                        if (indexWriter == null)
                            indexWriter = writeOperation.Value;

                        if (Log.IsDebugEnabled)
                            Log.Debug($"Executing map for '{_index.Name} ({_index.IndexId})'. Processing document: {document.Key}.");

                        stats.IndexingAttempts++;

                        count++;
                        lastEtag = document.Etag;

                        try
                        {
                            _index.HandleMap(document, indexWriter, indexContext);

                            stats.IndexingSuccesses++;
                        }
                        catch (Exception e)
                        {
                            stats.IndexingErrors++;
                            if (Log.IsWarnEnabled)
                                Log.WarnException($"Failed to execute mapping function on '{document.Key}' for '{_index.Name} ({_index.IndexId})'.", e);

                            stats.AddMapError(document.Key, $"Failed to execute mapping function on {document.Key}. Message: {e.Message}");
                        }

                        if (sw.Elapsed > timeoutProcessing)
                        {
                            break;
                        }
                    }
                }

                if (count == 0)
                    continue;

                if (lastEtag <= lastMappedEtag)
                    continue;

                if (Log.IsDebugEnabled)
                    Log.Debug($"Executing map for '{_index.Name} ({_index.IndexId})'. Processed {count} documents in '{collection}' collection in {sw.ElapsedMilliseconds:#,#;;0} ms.");

                _indexStorage.WriteLastMappedEtag(indexContext.Transaction, collection, lastEtag);

                moreWorkFound = true;
            }

            return moreWorkFound;
        }
    }
}