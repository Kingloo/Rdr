using System;
using System.Collections.Generic;

namespace RdrLib.Common
{
	public sealed class EmptyList<T> : List<T>
	{
		private static readonly EmptyList<T> emptyList = new EmptyList<T>();

		public EmptyList()
		{
			Capacity = 0;
		}

#pragma warning disable CA1822 // Mark members as static
		internal new void Add(T t)
#pragma warning restore CA1822 // Mark members as static
		{
			throw new InvalidOperationException("cannot .Add to EmptyList<T>");
		}

#pragma warning disable CA1000 // Do not declare static members on generic types
		public static EmptyList<T> Empty { get => emptyList; }
#pragma warning restore CA1000 // Do not declare static members on generic types
	}
}
