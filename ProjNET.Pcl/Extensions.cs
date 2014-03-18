using System;
using System.Collections.Generic;
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;

namespace ProjNet.CoordinateSystems
{
	/// <summary>
	/// PCL Extension methods
	/// </summary>
	public static class PCLExtensions
	{
		internal static Parameter Find(this List<Parameter> items, Predicate<Parameter> match)
		{
			foreach (Parameter item in items)
			{
				if (match(item))
					return item;
			}
			return null;
		}

		internal static ProjectionParameter Find(this List<ProjectionParameter> items, Predicate<ProjectionParameter> match)
		{
			foreach (ProjectionParameter item in items)
			{
				if (match(item))
					return item;
			}
			return null;
		}

		//public static T Find(this List<T> items, Predicate<T> match)
		//{
		//    foreach (T item in items)
		//    {
		//        if (match(item))
		//            return item;
		//    }
		//    return default(T);
		//}
	}
}
