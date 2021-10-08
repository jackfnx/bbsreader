using BBSReader.Data;
using System.Collections.Generic;
using System.Linq;

namespace BBSReader
{
    class Grouper
    {
        private static List<T> in_group<T>(T tid, List<List<T>> groups)
        {
            foreach (var group in groups)
            {
                if (group.Contains(tid))
                    return group;
            }
            return null;
        }

        public static void GroupingSuperKeyword(MetaData metaData)
        {
            for (int i = 0; i < metaData.superKeywords.Count(); i++)
            {
                var sk = metaData.superKeywords[i];
                var tids = sk.tids;
                var groups = sk.groups;
                List<string> keys = new List<string>();
                foreach (int tid in tids)
                {
                    var t = metaData.threads[tid];
                    string key = t.siteId + "/" + t.threadId;
                    keys.Add(key);
                }
                List<List<int>> id_groups = new List<List<int>>();
                for (int j = 0; j < tids.Count; j++)
                {
                    int tid = tids[j];
                    string key = keys[j];
                    if (in_group(tid, id_groups) != null)
                        continue;
                    List<int> id_group = new List<int> { tid };
                    List<string> key_group = in_group(key, groups);
                    if (key_group != null)
                    {
                        foreach (var anotherKey in key_group)
                        {
                            if (anotherKey != key)
                            {
                                int k = keys.IndexOf(anotherKey);
                                id_group.Add(tids[k]);
                            }
                        }
                    }
                    id_groups.Add(id_group);
                }
                sk.groupedTids = new List<Group>();
                foreach (var id_group in id_groups)
                {
                    var group_keys = id_group.ConvertAll(x => keys[tids.IndexOf(x)]);
                    var key_group = in_group(group_keys[0], groups);
                    id_group.Sort((x, y) => key_group.IndexOf(group_keys[id_group.IndexOf(x)]) - key_group.IndexOf(group_keys[id_group.IndexOf(y)]));
                    List<string> lines = id_group.ConvertAll(x =>
                    {
                        var t = metaData.threads[x];
                        string siteName = Constants.SITE_DEF[t.siteId].siteName;
                        return string.Format("[{0}]\t<{1}>\t{2}", t.title, t.author, siteName);
                    });
                    sk.groupedTids.Add(new Group { exampleId = id_group[0], tooltips = string.Join("\n", lines) });
                }
                metaData.superKeywords[i] = sk;
            }
        }

    }
}
