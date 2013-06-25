using System;
using System.Collections.Generic;
using System.Text;

namespace Signum.Utilities
{
    public class StringDistance
    {
        int[][] num;

        public int LevenshteinDistance(string str1, string str2, Func<char, int> deleteWeight = null, Func<char, int> insertWeight = null, Func<char, char, int> replaceWeight = null)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return 0;

            int M1 = str1.Length + 1;
            int M2 = str2.Length + 1;

            ResizeArray(M1, M2);

            for (int i = 0; i < M1; i++)
                num[i][0] = i;
            for (int j = 0; j < M2; j++)
                num[0][j] = j;

            for (int i = 1; i < M1; i++)
            {
                char cs = str1[i - 1];

                for (int j = 1; j < M2; j++)
                {
                    if (cs == str2[j - 1])
                        num[i][j] = num[i - 1][j - 1];
                    else
                        num[i][j] = Math.Min(Math.Min(
                            num[i - 1][j] + (deleteWeight != null ? deleteWeight(cs) : 1),        //deletion
                            num[i][j - 1] + (insertWeight != null ? insertWeight(cs) : 1)),       //insertion
                            num[i - 1][j - 1] + (replaceWeight != null ? replaceWeight(cs, str2[j - 1]) : 1)); //replace
                }
            }

            return num[M1 - 1][M2 - 1];
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
                        num[i][j] = 0;
                    else
                    {
                        if ((i == 0) || (j == 0))
                            num[i][j] = 1;
                        else
                            num[i][j] = 1 + num[i - 1][j - 1];

                        if (num[i][j] > maxlen)
                        {
                            maxlen = num[i][j];
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
                num[i][0] = 0;
            for (int j = 0; j < M2; j++)
                num[0][j] = 0;

            for (int i = 1; i < M1; i++)
            {
                for (int j = 1; j < M2; j++)
                {
                    if (str1[i - 1] == str2[j - 1])
                        num[i][j] = num[i - 1][j - 1] + 1;
                    else
                    {
                        if (num[i][j - 1] > num[i - 1][j])
                            num[i][j] = num[i][j - 1];
                        else
                            num[i][j] = num[i - 1][j];
                    }
                }
            }

            return num[str1.Length][str2.Length];
        }

        private void ResizeArray(int M1, int M2)
        {
            if (num == null || M1 > num.Length || M2 > num[0].Length)
            {
                num = new int[M1][];

                for (int i = 0; i < M1; i++)
                {
                    num[i] = new int[M2];
                }
            }
        }
    }
}
