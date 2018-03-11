using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace sync_youtube_dl
{
    class SongParser
    {
        public static string normalize(string input)
        {
            //Lower case all
            string output = input.ToLower().Trim();

            //Remove quotation marks
            output = output.Replace("\"", "");

            //Remove tags in parenteses, or if they contain important info, expose the tag
            string[] importantTags = { " ft", " feat", " featuring" };
            if (ContainsBetween(output, '(', ')', importantTags))
            {
                output = output.Replace("(", "");
                output = output.Replace(")", "");
            }
            else
            {
                output = RemoveBetween(output, '(', ')');
            }

            if (ContainsBetween(output, '[', ']', importantTags))
            {
                output = output.Replace("[", "");
                output = output.Replace("]", "");
            }
            else
            {
                output = RemoveBetween(output, '[', ']');
            }

            //Normalize separation
            output = output.Replace("|", " - ").Replace(":", " - ");

            //Remove useless tags
            string[] baddies = { "| Worlds 2014 - League of Legends", ", pt. ii", "2006 Eurovision Song Contest Winner" };
            foreach (var bad in baddies)
            {
                output = output.Replace(bad.Trim().ToLower(), " ");
            }

            //Secondly normalize featuring tag
            string[] featuringtags = { "ft", "feat", "featuring" };

            foreach (var feat in featuringtags)
            {
                string replaceFeatTo = " ft.";
                if (output.Contains(feat + "."))
                {
                    output = output.Replace(" " + feat + ".", replaceFeatTo);
                }
                else
                {
                    output = output.Replace(" " + feat, replaceFeatTo);
                }
            }

            //replace other characters
            output = output.Replace("‒", " - ");

            //Remove third part (which is usualy useless information)
            string[] ignoreTP = { "anne-marie", "potential_string_to_ignore" };
            try
            {
                bool b_ignoreTP = false;
                foreach (var s in ignoreTP)
                {
                    if(output.Contains(s))
                    {
                        b_ignoreTP = true;
                    }
                }

                if (!b_ignoreTP)
                {
                    int i = IndexOfNth(output, "-", 2);
                    output = output.Remove(i, output.Length - i).Trim();
                }
            }
            catch { };

            //Normalize list of artists
            string[] ignoreAN = { "potential_string_to_ignore0", "potential_string_to_ignore" };
            try
            {
                bool b_ignoreAN = false;
                foreach (var s in ignoreAN)
                {
                    if (output.Contains(s))
                    {
                        b_ignoreAN = true;
                    }
                }

                if (!b_ignoreAN)
                {
                    output = output.Substring(0,output.IndexOf('-')).Replace(",", " & ") + output.Substring(output.IndexOf('-'));
                }
            }
            catch { };

            //Replace certain elements
            output = output.Replace("a_tag_to_replace", "abc").Replace("another_tag_to_replace", "123");
            
            //Remove duplicated sucessive spaces
            output = Regex.Replace(output.Trim(), @"\s+", " ");

            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
            TextInfo textInfo = cultureInfo.TextInfo;
            output = textInfo.ToTitleCase(output);


            return output;
        }

        public static List<string> toListArtists(string title)
        {
            List<string> artists = new List<string>();
            try
            {
                string artistFull = title.Split('-')[0];
                artists = new List<string>(title.Split('&'));
                for (int i = 0; i < artists.Count; i++)
                {
                    artists[i] = artists[i].Trim();
                }
            }
            catch
            {
                artists.Add(title);
            }
            return artists;
        }

        private static string RemoveBetween(string s, char begin, char end)
        {
            Regex regex = new Regex(string.Format("\\{0}.*?\\{1}", begin, end));
            return regex.Replace(s, string.Empty);
        }

        private static bool ContainsBetween(string s, char begin, char end, string[] contain)
        {
            Regex regex = new Regex(string.Format("\\{0}.*?\\{1}", begin, end));
            string s1 = regex.Replace(s, string.Empty);

            List<string> diff;
            IEnumerable<string> set1 = s1.Split(' ').Distinct();
            IEnumerable<string> set2 = s.Split(' ').Distinct();
            diff = set2.Except(set1).ToList();
            foreach (var d in diff)
            {
                foreach (var c in contain)
                {
                    if (d.Contains(c))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static int IndexOfNth(string str, string value, int nth = 1)
        {
            if (nth <= 0)
                throw new ArgumentException("Can not find the zeroth index of substring in string. Must start with 1");
            int offset = str.IndexOf(value);
            for (int i = 1; i < nth; i++)
            {
                if (offset == -1) return -1;
                offset = str.IndexOf(value, offset + 1);
            }
            return offset;
        }

        public static string toUtf8(string input)
        {
            byte[] bytes = Encoding.Default.GetBytes(input);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
