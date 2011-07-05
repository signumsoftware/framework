using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Utilities
{
    public static class ArrayExtensions
    {
        public static T[,] Initialize<T>(this T[,] array, Func<int, int, T> valueXY)
        {
            for (int j = 0; j < array.GetLength(1); j++)
                for (int i = 0; i < array.GetLength(0); i++)
                    array[i, j] = valueXY(i, j);

            return array;
        }

        public static T[, ,] Initialize<T>(this T[, ,] array, Func<int, int, int, T> valueXYZ)
        {
            for (int k = 0; k < array.GetLength(2); k++)
                for (int j = 0; j < array.GetLength(1); j++)
                    for (int i = 0; i < array.GetLength(0); i++)
                        array[i, j, k] = valueXYZ(i, j, k);

            return array;
        }

        public static S[] ToArray<T, S>(this IEnumerable<T> collection, Func<T, S> value, Func<T, int> xPos)
        {
            if (collection.IsEmpty())
                return new S[0];

            return ToArray(collection, value, xPos, collection.Max(xPos) + 1);
        }

        public static S[] ToArray<T, S>(this IEnumerable<T> collection, Func<T, S> value, Func<T, int> xPos, int xLength)
        {
            S[] result = new S[xLength];
            foreach (var item in collection)
                result[xPos(item)] = value(item);
            return result;
        }

        public static S[,] ToArray<T, S>(this IEnumerable<T> collection, Func<T, S> value, Func<T, int> xPos, Func<T, int> yPos)
        {
            if (collection.IsEmpty())
                return new S[0, 0];

            return ToArray(collection, value, xPos, yPos, collection.Max(xPos) + 1, collection.Max(yPos) + 1);
        }

        public static S[,] ToArray<T, S>(this IEnumerable<T> collection, Func<T, S> value, Func<T, int> xPos, Func<T, int> yPos, int xLength, int yLength)
        {
            S[,] result = new S[xLength, yLength];
            foreach (var item in collection)
                result[xPos(item), yPos(item)] = value(item);
            return result;
        }

        public static S[, ,] ToArray<T, S>(this IEnumerable<T> collection, Func<T, S> value, Func<T, int> xPos, Func<T, int> yPos, Func<T, int> zPos)
        {
            if (collection.IsEmpty())
                return new S[0, 0, 0];

            return ToArray(collection, value, xPos, yPos, zPos, collection.Max(xPos) + 1, collection.Max(yPos) + 1, collection.Max(zPos) + 1);
        }

        public static S[, ,] ToArray<T, S>(this IEnumerable<T> collection, Func<T, S> value, Func<T, int> xPos, Func<T, int> yPos, Func<T, int> zPos, int xLength, int yLength, int zLength)
        {
            S[, ,] result = new S[xLength, yLength, zLength];
            foreach (var item in collection)
                result[xPos(item), yPos(item), zPos(item)] = value(item);
            return result;
        }

        public static IEnumerable<T> Row<T>(this T[,] data, int row)
        {
            for (int i = 0; i < data.GetLength(0); i++)
                yield return data[i, row];
        }

        public static IEnumerable<T> Column<T>(this T[,] data, int column)
        {
            for (int j = 0; j < data.GetLength(1); j++)
                yield return data[column, j];
        }

        public static T[,] AddRow<T>(this T[,] values, int pos, T[] newValues)
        {
            return AddRow(values, pos, i => newValues[i]);
        }

        public static T[,] AddRow<T>(this T[,] values, int pos, Func<int, T> newValue)
        {
            int width = values.GetLength(0);
            int height = values.GetLength(1);
            T[,] result = new T[width, height + 1];
            for (int j = 0; j < pos; j++)
                for (int i = 0; i < width; i++)
                    result[i, j] = values[i, j];

            for (int i = 0; i < width; i++)
                result[i, pos] = newValue(i);

            for (int j = pos; j < height; j++)
                for (int i = 0; i < width; i++)
                    result[i, j + 1] = values[i, j];
            return result;
        }

        public static T[,] AddColumn<T>(this T[,] values, int pos, T[] newValues)
        {
            return AddColumn(values, pos, i => newValues[i]);
        }

        public static T[,] AddColumn<T>(this T[,] values, int pos, Func<int, T> newValue)
        {
            int width = values.GetLength(0);
            int height = values.GetLength(1);
            T[,] result = new T[width + 1, height];
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < pos; i++)
                    result[i, j] = values[i, j];
                result[pos, j] = newValue(j);
                for (int i = pos; i < width; i++)
                    result[i + 1, j] = values[i, j];
            }
            return result;
        }

        public static S[,] SelectArray<T, S>(this T[,] values, Func<T, S> selector)
        {
            return new S[values.GetLength(0), values.GetLength(1)].Initialize((i, j) => selector(values[i, j]));
        }

        public static S[,] SelectArray<T, S>(this T[,] values, Func<int, int, T, S> selector)
        {
            return new S[values.GetLength(0), values.GetLength(1)].Initialize((i, j) => selector(i, j, values[i, j]));
        }

        public static S[, ,] SelectArray<T, S>(this T[, ,] values, Func<T, S> selector)
        {
            return new S[values.GetLength(0), values.GetLength(1), values.GetLength(2)].Initialize((i, j, k) => selector(values[i, j, k]));
        }

        public static S[, ,] SelectArray<T, S>(this T[, ,] values, Func<int, int, int, T, S> selector)
        {
            return new S[values.GetLength(0), values.GetLength(1), values.GetLength(2)].Initialize((i, j, k) => selector(i, j, k, values[i, j, k]));
        }
    }
}
