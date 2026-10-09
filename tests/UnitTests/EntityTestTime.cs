using System.Reflection;
using Core.Domain.Common;
namespace UnitTests;
internal static class EntityTestTime
{
    // Fixture-only timestamps keep same-day ordering tests deterministic without changing the immutable domain API.
    public static T At<T>(T entity, DateTime createdAt) where T : BaseEntity
    {
        typeof(BaseEntity).GetField("<CreatedAt>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(entity, createdAt);
        return entity;
    }
}
