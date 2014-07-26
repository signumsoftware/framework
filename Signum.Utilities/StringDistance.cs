using System;
using System.Collections.Generic;
using System.Text;

namespace Signum.Utilities
{
    public delegate int CharWeighter(char? c1, char? c2);

    public class StringDistance
    {
        int[,] num;

        public int LevenshteinDistance(string str1, string str2, CharWeighter weighter = null)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return 0;

            if (weighter == null)
                weighter = (c1, c2) => 1;

            int M1 = str1.Length + 1;
            int M2 = str2.Length + 1;

            ResizeArray(M1, M2);

            num[0, 0] = 0;

            for (int i = 1; i < M1; i++)
                num[i, 0] = num[i - 1, 0] + weighter(str1[i - 1], null);
            for (int j = 1; j < M2; j++)
                num[0, j] = num[0, j - 1] + weighter(null, str2[j - 1]);

            for (int i = 1; i < M1; i++)
            {
                for (int j = 1; j < M2; j++)
                {
                    if (str1[i - 1] == str2[j - 1])
                        num[i, j] = num[i - 1, j - 1];
                    else
                        num[i, j] = Math.Min(Math.Min(
                            num[i - 1, j] + weighter(str1[i - 1], null),        
                            num[i, j - 1] + weighter(null, str2[j - 1])),       
                            num[i - 1, j - 1] + weighter(str1[i - 1], str2[j - 1])); 
                }
            }

            return num[M1 - 1, M2 - 1];
        }

        public int LongestCommonSubstring(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return 0;

            ResizeArray(str1.Length, str2.Length);

            int maxlen = 0;

            for (int i = 0; i < str1.Length; i++)
            {
                for (int j = 0; j < str2.Length; j++)
                {
                    if (str1[i] != str2[j])
                        num[i, j] = 0;
                    else
                    {
                        if ((i == 0) || (j == 0))
                            num[i, j] = 1;
                        else
                            num[i, j] = 1 + num[i - 1, j - 1];

                        if (num[i, j] > maxlen)
                        {
                            maxlen = num[i, j];
                        }
                    }
                }
            }
            return maxlen;
        }

        public int LongestCommonSubsequence(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return 0;

            int M1 = str1.Length + 1;
            int M2 = str2.Length + 1;

            ResizeArray(M1, M2);

            for (int i = 0; i < M1; i++)
                num[i, 0] = 0;
            for (int j = 0; j < M2; j++)
                num[0, j] = 0;

            for (int i = 1; i < M1; i++)
            {
                for (int j = 1; j < M2; j++)
                {
                    if (str1[i - 1] == str2[j - 1])
                        num[i, j] = num[i - 1, j - 1] + 1;
                    else
                    {
                        if (num[i, j - 1] > num[i - 1, j])
                            num[i, j] = num[i, j - 1];
                        else
                            num[i, j] = num[i - 1, j];
                    }
                }
            }

            return num[str1.Length, str2.Length];
        }

        private void ResizeArray(int M1, int M2)
        {
            if (num == null || M1 > num.GetLength(0) || M2 > num.GetLength(1))
            {
                num = new int[M1, M2];
            }
        }
    }
}
