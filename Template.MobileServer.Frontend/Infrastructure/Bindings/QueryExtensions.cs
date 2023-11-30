namespace Template.MobileServer.Frontend.Infrastructure.Bindings;

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Extensions.Primitives;

public static class QueryExtensions
{
    private static readonly ConcurrentDictionary<Type, object> Bindings = new();

    public static T Bind<T>(this Dictionary<string, StringValues> query)
    {
        var binding = (BindingInfo<T>)Bindings.GetOrAdd(typeof(T), static x =>
        {
            var ci = x.GetConstructor(Type.EmptyTypes);
            if (ci is null)
            {
                throw new NotSupportedException($"Default constructor is required. type=[{x}]");
            }

            var factory = CreateFactory<T>(ci);

            var mappers = new List<IPropertyMapper<T>>();
            foreach (var pi in x.GetProperties())
            {
                if (!pi.CanWrite)
                {
                    continue;
                }

                if (pi.PropertyType == typeof(string))
                {
                    mappers.Add(new StringPropertyMapper<T>(pi));
                }
                else
                {
                    var mapperType = typeof(ConvertPropertyMapper<,>).MakeGenericType(x, pi.PropertyType);
                    mappers.Add((IPropertyMapper<T>)Activator.CreateInstance(mapperType, pi)!);
                }
            }

            return new BindingInfo<T>
            {
                Factory = factory,
                Mappers = mappers.ToArray()
            };
        });

        var instance = binding.Factory();
        foreach (var mapper in binding.Mappers)
        {
            mapper.Map(instance, query);
        }

        return instance;
    }

#pragma warning disable SA1401 // Fields should be private
    private sealed class BindingInfo<T>
    {
        public Func<T> Factory = default!;

        public IPropertyMapper<T>[] Mappers = default!;
    }
#pragma warning restore SA1401 // Fields should be private

    private interface IPropertyMapper<in T>
    {
        void Map(T instance, Dictionary<string, StringValues> query);
    }

    private sealed class StringPropertyMapper<T> : IPropertyMapper<T>
    {
        private readonly string name;

        private readonly Action<T, string?> setter;

        public StringPropertyMapper(PropertyInfo pi)
        {
            name = pi.Name;
            setter = CreateSetter<T, string?>(pi);
        }

        public void Map(T instance, Dictionary<string, StringValues> query)
        {
            if (query.TryGetValue(name, out var value))
            {
                setter(instance, value);
            }
        }
    }

#pragma warning disable CA1812
    private sealed class ConvertPropertyMapper<T, TProperty> : IPropertyMapper<T>
    {
        private readonly string name;

        private readonly Action<T, TProperty> setter;

        public ConvertPropertyMapper(PropertyInfo pi)
        {
            name = pi.Name;
            setter = CreateSetter<T, TProperty>(pi);
        }

        public void Map(T instance, Dictionary<string, StringValues> query)
        {
            if (query.TryGetValue(name, out var value) &&
                ConvertHelper.Converter<TProperty>.TryConverter(value, out var result))
            {
                setter(instance, result);
            }
        }
    }
#pragma warning restore CA1812

    private static Func<TTarget> CreateFactory<TTarget>(ConstructorInfo ci)
    {
        return Expression.Lambda<Func<TTarget>>(Expression.New(ci)).Compile();
    }

    private static Action<TTarget, TMember> CreateSetter<TTarget, TMember>(PropertyInfo pi)
    {
        var parameterExpression = Expression.Parameter(typeof(TTarget));
        var parameterExpression2 = Expression.Parameter(typeof(TMember));
        var propertyExpression = Expression.Property(parameterExpression, pi);
        return Expression.Lambda<Action<TTarget, TMember>>(
            Expression.Assign(propertyExpression, parameterExpression2),
            parameterExpression,
            parameterExpression2).Compile();
    }

    public static bool TryGetValue<T>(this Dictionary<string, StringValues> query, string key, out T result)
    {
        if (query.TryGetValue(key, out var value) &&
            ConvertHelper.Converter<T>.TryConverter(value, out result))
        {
            return true;
        }

        result = default!;
        return false;
    }

    public static T GetValueOrDefault<T>(this Dictionary<string, StringValues> query, string key, T defaultValue = default!)
    {
        if (query.TryGetValue(key, out var value) &&
            ConvertHelper.Converter<T>.TryConverter(value, out var result))
        {
            return result;
        }

        return defaultValue;
    }

    public static T[] GetValuesOrDefault<T>(this Dictionary<string, StringValues> query, string key)
    {
        if (query.TryGetValue(key, out var values))
        {
            var list = new List<T>(values.Count);
            foreach (var value in values)
            {
                if (ConvertHelper.Converter<T>.TryConverter(value, out var result))
                {
                    list.Add(result);
                }
            }

            return list.ToArray();
        }

        return [];
    }
}
