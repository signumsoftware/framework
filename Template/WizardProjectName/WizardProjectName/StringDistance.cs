using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WizardProjectName
{
    public class StringDistance
    {
        int[][] num;

        public int Distance(string s, string t)
        {
            int Ms = s.Length + 1;
            int Mt = t.Length + 1;


            if (num == null || Ms > num.Length || Mt > num[0].Length)
            {

                num = new int[Ms][];

                for (int i = 0; i < Ms; i++)
                {
                    num[i] = new int[Mt];
                    num[i][0] = i;
                }

                for (int j = 0; j < Mt; j++)
                    num[0][j] = j;
            }

            for (int i = 1; i < Ms; i++)
            {
                char cs = s[i - 1];
                int[] numim1 = num[i - 1];
                int[] numi = num[i];
                for (int j = 1; j < Mt; j++)
                {
                    num[i][j] = Math.Min(Math.Min(numim1[j] + 1, numi[j - 1] + 1), numim1[j - 1] + ((cs == t[j - 1]) ? 0 : 1));
                }
            }

            return num[Ms - 1][Mt - 1];

        }
    }
}
