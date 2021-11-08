using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace vivego.KeyValue.Sql
{
	public sealed class StateDbContext : DbContext
	{
		public string TableName { get; }

		public DbSet<StateEntry> States { get; set; } = default!;

		public StateDbContext(string tableName,
			DbContextOptions<StateDbContext> options) : base(options)
		{
			if (string.IsNullOrEmpty(tableName)) throw new ArgumentException("Value cannot be null or empty.", nameof(tableName));
			TableName = tableName;
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (optionsBuilder is null) throw new ArgumentNullException(nameof(optionsBuilder));
			optionsBuilder.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>();
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			if (modelBuilder is null) throw new ArgumentNullException(nameof(modelBuilder));

			modelBuilder.Entity<StateEntry>().ToTable(TableName);
			modelBuilder.Entity<StateEntry>().HasKey(c => c.Id);
			modelBuilder.Entity<StateEntry>()
				.Property(e => e.Id)
				.HasColumnName("id")
				.ValueGeneratedNever();
			modelBuilder.Entity<StateEntry>()
				.Property(e => e.Data)
				.HasColumnName("data");
			modelBuilder.Entity<StateEntry>()
				.Property(e => e.Etag)
				.HasColumnName("etag");
			modelBuilder.Entity<StateEntry>()
				.Property(e => e.ExpiresAt)
				.HasColumnName("expires_at");
		}
	}
}
