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

        public static void GroupingSuperKeywords(MetaData metaData)
        {
            for (int i = 0; i < metaData.superKeywords.Count(); i++)
            {
                GroupingSuperKeyword(metaData, i);
            }
        }

        public static void GroupingSuperKeyword(MetaData metaData, int i)
        {
            var sk = metaData.superKeywords[i];
            var tids = sk.tids;
            var groups = sk.groups;
            var kws = sk.kws;
            List<string> keys = new List<string>();
            foreach (int tid in tids)
            {
                var t = metaData.threads[tid];
                string key = t.siteId + "/" + t.threadId;
                keys.Add(key);
            }
            ComparerBuilder cb = new ComparerBuilder() { tids = tids, keys = keys, groups = groups };
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
            sk.groupedTids = getGroupedTids(id_groups, metaData.threads, cb);
            sk.subSKs = new List<SuperKeyword>();
            if (sk.skType == SKType.Author)
            {
                List<List<List<int>>> corpus_kwgroups = new List<List<List<int>>>();
                List<List<int>> corpus_nokwgroups = new List<List<int>>();
                sk.subKeywords.ForEach(x => corpus_kwgroups.Add(new List<List<int>>()));
                for (int j = 0; j < tids.Count; j++)
                {
                    var tid = tids[j];
                    var kw = kws[j];
                    var id_group = in_group(tid, id_groups);
                    if (kw.Count > 0)
                    {
                        foreach (int kw_id in kw)
                        {
                            if (!corpus_kwgroups[kw_id].Contains(id_group))
                            {
                                corpus_kwgroups[kw_id].Add(id_group);
                            }
                        }
                    }
                    else
                    {
                        if (id_group.All(x => kws[tids.IndexOf(x)].Count == 0) && !corpus_nokwgroups.Contains(id_group))
                        {
                            corpus_nokwgroups.Add(id_group);
                        }
                    }
                }
                for (int j = 0; j < sk.subKeywords.Count; j++)
                {
                    var kw_id_groups = corpus_kwgroups[j];
                    var keyword = sk.subKeywords[j][0];
                    var read = sk.subReads[j];
                    sk.subSKs.Add(new SuperKeyword()
                    {
                        skType = SKType.Simple,
                        keyword = keyword,
                        authors = sk.authors,
                        alias = new List<string>(),
                        tids = new List<int>(),
                        groups = new List<List<string>>(),
                        kws = new List<List<int>>(),
                        read = read,
                        subReads = new List<int>(),
                        groupedTids = getGroupedTids(kw_id_groups, metaData.threads, cb),
                        subSKs = new List<SuperKeyword>(),
                        noSKGTids = new List<Group>()
                    });
                }
                sk.noSKGTids = getGroupedTids(corpus_nokwgroups, metaData.threads, cb);
            }
            else
            {
                sk.noSKGTids = new List<Group>();
            }
            metaData.superKeywords[i] = sk;
        }

        private static List<Group> getGroupedTids(List<List<int>> id_groups, List<BBSThread> threads, ComparerBuilder cb)
        {
            List<Group> groupedTids = new List<Group>();
            foreach (var id_group in id_groups)
            {
                id_group.Sort(cb.GetComparer(id_group));
                List<string> lines = id_group.ConvertAll(x =>
                {
                    var t = threads[x];
                    string siteName = Constants.SITE_DEF[t.siteId].siteName;
                    return string.Format("[{0}]\t<{1}>\t{2}", t.title, t.author, siteName);
                });
                groupedTids.Add(new Group { exampleId = id_group[0], tooltips = string.Join("\n", lines) });
            }
            return groupedTids;
        }

        private class ComparerBuilder
        {
            public List<int> tids;
            public List<string> keys;
            public List<List<string>> groups;

            internal IComparer<int> GetComparer(List<int> id_group)
            {
                return new GroupIdComparer() { id_group = id_group, tids = tids, keys = keys, groups = groups };
            }

            internal class GroupIdComparer : IComparer<int>
            {
                public List<int> id_group;
                public List<int> tids;
                public List<string> keys;
                public List<List<string>> groups;

                public int Compare(int x, int y)
                {
                    var group_keys = id_group.ConvertAll(i => keys[tids.IndexOf(i)]);
                    var key_group = in_group(group_keys[0], groups);
                    return key_group.IndexOf(group_keys[id_group.IndexOf(x)]) - key_group.IndexOf(group_keys[id_group.IndexOf(y)]);
                }
            }
        }
    }
}
