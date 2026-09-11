using System;

namespace Prototype.Application
{
    /// <summary>
    /// Small generic projection component used at Application boundaries. It keeps raw Domain
    /// aggregates out of UI code without creating one wrapper snapshot class per feature.
    /// </summary>
    public static class GenericMapper
    {
        public static TDestination Map<TSource, TDestination>(
            TSource source,
            Func<TSource, TDestination> projection)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            return projection(source);
        }
    }
}
