﻿// -----------------------------------------------------------------------
//  <copyright file="DeletionsSmuggler.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------
using System.Threading;
using System.Threading.Tasks;

using Raven.Abstractions.Database.Smuggler.Database;

namespace Raven.Smuggler.Database
{
	internal class DocumentDeletionsSmuggler : SmugglerBase
	{
		private readonly DatabaseLastEtagsInfo _maxEtags;

		public DocumentDeletionsSmuggler(DatabaseSmugglerOptions options, DatabaseSmugglerNotifications notifications, IDatabaseSmugglerSource source, IDatabaseSmugglerDestination destination, DatabaseLastEtagsInfo maxEtags)
			: base(options, notifications, source, destination)
		{
			_maxEtags = maxEtags;
		}

		public override async Task SmuggleAsync(DatabaseSmugglerOperationState state, CancellationToken cancellationToken)
		{
			using (var actions = Destination.DocumentDeletionActions())
			{
				if (Source.SupportsDocumentDeletions == false)
					return;

				if (Options.OperateOnTypes.HasFlag(DatabaseItemType.Documents) == false)
				{
					await Source.SkipDocumentDeletionsAsync(cancellationToken).ConfigureAwait(false);
					return;
				}

				var enumerator = await Source
					.ReadDocumentDeletionsAsync(state.LastDocDeleteEtag, _maxEtags.LastDocDeleteEtag.IncrementBy(1), cancellationToken)
					.ConfigureAwait(false);

				while (await enumerator.MoveNextAsync().ConfigureAwait(false))
				{
					await actions
						.WriteDocumentDeletionAsync(enumerator.Current, cancellationToken)
						.ConfigureAwait(false);
				}
			}
		}
	}
}