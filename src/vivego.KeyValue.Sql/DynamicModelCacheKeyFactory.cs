using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace vivego.KeyValue.Sql
{
	public sealed class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
	{
		public object Create(DbContext context, bool designTime)
		{
			if (context is null) throw new ArgumentNullException(nameof(context));
			return context is StateDbContext dynamicContext
				? (context.GetType(), dynamicContext.TableName, designTime)
				: context.GetType();
		}

		public object Create(DbContext context)
			=> Create(context, false);
	}
}
