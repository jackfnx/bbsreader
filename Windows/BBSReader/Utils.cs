using BBSReader.Data;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BBSReader
{
    class Utils
    {
        public static long GetTimestamp(string s)
        {
            DateTime t = DateTime.Parse(s);
            DateTime t0 = new DateTime(1970, 1, 1, 0, 0, 0);
            return (t.Ticks - t0.Ticks) / 1000 / 10000;
        }

        public static string CalcKey(string title, string author, bool simple)
        {
            string rawSign = string.Format("{0}/{1}/{2}", title, author, simple);
            return Utils.GenerateMD5(rawSign);
        }

        public static string CalcSumary(string title, string author, bool simple, List<Chapter> chapters, byte[] cover)
        {
            string chaptersSummary = string.Join(":", chapters.ConvertAll(x => x.savePath));
            string rawSign = string.Format("{0}/{1}/{2}:{3}", title, author, simple, chaptersSummary);
            if (cover != null)
            {
                rawSign += ":" + Utils.Hash(cover);
            }
            return Utils.GenerateMD5(rawSign);
        }

        public static string GenerateMD5(string s)
        {
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            byte[] data = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(s));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        private static string Hash(byte[] raw)
        {
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            byte[] data = md5Hasher.ComputeHash(raw);
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
    }
}
