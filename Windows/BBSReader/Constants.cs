using BBSReader.Data;
using System.Collections.Generic;

namespace BBSReader
{
    class Constants
    {
        public const string LOCAL_PATH = "C:/Users/hpjing/Dropbox/BBSReader.Cache/";

        public static readonly Dictionary<string, BBSDef> SITE_DEF = new Dictionary<string, BBSDef> {
            { "sis001", new BBSDef { siteId="sis001", siteName="第一会所", siteHost="https://www.sis001.com/forum/", id=0, onlineUpdate=true } },
            { "sexinsex", new BBSDef { siteId="sexinsex", siteName="色中色", siteHost="http://www.sexinsex.net/bbs/", id=1, onlineUpdate=true } },
            { "cool18", new BBSDef { siteId="cool18", siteName="禁忌书屋", siteHost="https://www.cool18.com/bbs4/index.php?", id=2, onlineUpdate=false } }
        };
    }
}
