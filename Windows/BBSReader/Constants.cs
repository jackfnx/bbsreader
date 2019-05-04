using BBSReader.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBSReader
{
    class Constants
    {
        public const string LOCAL_PATH = "C:/Users/hpjing/Dropbox/BBSReader.Cache/";

        public static readonly Dictionary<string, BBSDef> SITE_DEF = new Dictionary<string, BBSDef> {
            { "sis001", new BBSDef { siteId="sis001", siteName="第一会所", siteHost="http://www.sis001.com/forum/", id=0 } },
            { "sexinsex", new BBSDef { siteId="sexinsex", siteName="色中色", siteHost="http://www.sexinsex.net/bbs/", id=1 } }
        };
    }
}
