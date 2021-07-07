using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeWithNoTracking : IMerge
    {
        private readonly IMerge _merge;

        public MergeWithNoTracking(IMerge merge)
        {
            _merge = merge;
        }

        public MergeContext Context => _merge.Context;

        public MergeBehavior Behavior => _merge.Behavior;

        public IMergeOn On => _merge.On;

        public IMergeInsert Insert => _merge.Insert;

        public IMergeUpdate Update => _merge.Update;

        public IMergeOutput Output => _merge.Output;

        public IMergeSource Source => _merge.Source;

        public IEntityType Target => _merge.Target;

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await PreProcessAsync(cancellationToken);

            await _merge.ExecuteAsync(cancellationToken);

            await PostProcessAsync(cancellationToken);
        }

        protected virtual Task PreProcessAsync(CancellationToken cancellationToken = default)
        {
            var entities = Context.Get(Target.ClrType);
            var navigations = Target.GetNavigations().Where(navigation => navigation.IsDependentToPrincipal() && !navigation.DeclaringEntityType.IsOwned()).ToList();

            foreach (var entity in entities)
            foreach (var navigation in navigations)
            {
                var value = navigation.GetGetter().GetClrValue(entity);
                if (value == null)
                    continue;

                var foreignKey = navigation.ForeignKey.PrincipalKey.Properties.Select(property => property.GetGetter().GetClrValue(value)).ToArray();

                for (var index = 0; index < foreignKey.Length; index++)
                    navigation.ForeignKey.Properties[index].PropertyInfo.SetValue(entity, foreignKey[index]);
            }

            return Task.CompletedTask;
        }

        protected virtual async Task PostProcessAsync(CancellationToken cancellationToken = default)
        {
            var key = Target.FindPrimaryKey();
            var properties = On.Properties.Union(key.Properties).ToList();

            // TODO the required order here is hidden... make it clearer

            var statement = $"SELECT {string.Join(", ", properties.Select(property => $"[{property.GetColumnName()}]"))} FROM [{Output.GetTableName()}] WHERE [{Output.GetActionName()}] IN ('INSERT', 'UPDATE')";
            var connection = (SqlConnection) Context.Db.Database.GetDbConnection();
            var transaction = (SqlTransaction) Context.Db.Database.CurrentTransaction?.GetDbTransaction();

            await using var command = new SqlCommand(statement, connection, transaction);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!reader.HasRows)
                return;

            var entities = Context.Get(Target.ClrType).Cast<object>().ToLookup(entity => On.Comparer.GetHashCode(On.GetValues(entity)));
            var navigations = Target.GetNavigations().Where(navigation => !navigation.IsDependentToPrincipal() && !navigation.ForeignKey.DeclaringEntityType.IsOwned()).ToList();

            var raw = new object[properties.Count];
            var values = new object[properties.Count];
            var offset = properties.Count - key.Properties.Count;
            var on = new object[On.Properties.Count];

            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.GetValues(raw) != raw.Length)
                    throw new InvalidOperationException("Read an incorrect number of columns.");

                for (var index = 0; index < raw.Length; index++)
                {
                    var value = raw[index];

                    var property = properties[index];
                    var actual = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                    if (actual.IsEnum)
                        value = Enum.ToObject(actual, value);

                    var converter = property.GetValueConverter();
                    if (converter != null)
                        value = converter.ConvertFromProvider(value);

                    values[index] = value;
                }

                Array.Copy(values, on, on.Length);

                var entity = entities[On.Comparer.GetHashCode(on)].SingleOrDefault(obj => On.Comparer.Equals(On.GetValues(obj), on));
                if (entity == null)
                    throw new InvalidOperationException("Couldn't find the original entity.");

                for (var index = offset; index < properties.Count; index++)
                    properties[index].PropertyInfo.SetValue(entity, values[index]);

                foreach (var navigation in navigations)
                {
                    var value = navigation.GetGetter().GetClrValue(entity);
                    if (value == null)
                        continue;

                    if (navigation.IsCollection())
                    {
                        foreach (var item in (IEnumerable) value)
                        {
                            for (var index = 0; index < navigation.ForeignKey.Properties.Count; index++)
                                navigation.ForeignKey.Properties[index].PropertyInfo.SetValue(item, values[offset + index]);
                        }
                    }
                    else
                    {
                        for (var index = 0; index < navigation.ForeignKey.Properties.Count; index++)
                            navigation.ForeignKey.Properties[index].PropertyInfo.SetValue(value, values[offset + index]);
                    }
                }
            }
        }

        public override string ToString()
        {
            return _merge.ToString();
        }
    }
}