using System.Collections.Generic;

public static class ListExtensions{
	public static T pop<T>(this List<T> list)
	{
		T val = list [list.Count - 1];
		list.RemoveAt (list.Count - 1);
		return val;
	}

	public static void push<T>(this List<T> list, T val)
	{
		list.Add(val);
	}
	public static void remove<T>(this List<T> list, T val)
	{
		list.Remove(val);
	}
}
