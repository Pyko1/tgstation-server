﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// Access to running jobs
	/// </summary>
	public interface IJobsClient
	{
		/// <summary>
		/// List the <see cref="Api.Models.Internal.Job.Id"/>s in the <see cref="Instance"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of the <see cref="Api.Models.Internal.Job.Id"/>s in the <see cref="Instance"/></returns>
		Task<IReadOnlyList<Job>> List(CancellationToken cancellationToken);

		/// <summary>
		/// List the active <see cref="Job"/>s in the <see cref="Instance"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of the active <see cref="Job"/>s in the <see cref="Instance"/></returns>
		Task<IReadOnlyList<Job>> ListActive(CancellationToken cancellationToken);

		/// <summary>
		/// Get a <paramref name="job"/>
		/// </summary>
		/// <param name="job">The <see cref="Job"/> to get</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Job"/></returns>
		Task<Job> GetId(Job job, CancellationToken cancellationToken);

		/// <summary>
		/// Cancels a <paramref name="job"/>
		/// </summary>
		/// <param name="job">The <see cref="Job"/> to cancel</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Cancel(Job job, CancellationToken cancellationToken);
	}
}
