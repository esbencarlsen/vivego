using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace vivego.KeyValue.Sql
{
	public sealed class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
	{
		public object Create(DbContext context)
		{
			if (context is null) throw new ArgumentNullException(nameof(context));
			return context is StateDbContext dynamicContext
				? (context.GetType(), dynamicContext.TableName)
				: (object) context.GetType();
		}
	}
}
