import os
import platform
import re
import yaml
import json
import time
import requests
from utils import SK_Type, save_tags, load_tags, save_threads, load_threads, save_manual_topcis, load_manual_topics, _load_mts, _find_mt


save_root_path = "C:/Users/hpjing/Dropbox/BBSReader.Cache"
sysstr = platform.system()
if sysstr == "Windows":
    pass
elif sysstr == 'Linux' or sysstr == 'Darwin':
    save_root_path = '/Users/apple/Library/CloudStorage/Dropbox/BBSReader.Cache'
else:
    raise "Unknown system <%s>." % sysstr


class SexInSex_Login:
    def __init__(self, save_root_path):
        passwd_yaml = os.path.join(save_root_path, "PASSWORD.yaml")
        with open(passwd_yaml) as f:
            passwd = yaml.load(f, Loader=yaml.FullLoader)
            self.name = passwd["SexInSex"]["name"]
            self.pw = passwd["SexInSex"]["pw"]

    def login(self, session, head, proxy):
        loginurl = "http://www.sexinsex.net/bbs/logging.php?action=login&"
        loginparams = {
            'formhash': 'f33adb50',
            'referer': 'http://www.sexinsex.net/bbs/forum-372-1.html',
            'loginfield': 'username',
            'username': self.name,
            'password': self.pw,
            'questionid': 0,
            'answer': '',
            'cookietime': 315360000,
            'loginmode': 'normal',
            'styleid': 0,
            'loginsubmit': True
        }

        resp = session.post(loginurl, data=loginparams, headers=head, proxies=proxy)
        print("[色中色] 登陆: %s" % (resp))


### BBS列表
bbsdef = [
    [
        "第一会所",
        "sis001",
        "http://www.sis001.com/forum/",
        "forum-%d-%d.html",
        None,
        "utf-8",
        [383, 322],
    ],
    [
        "色中色",
        "sexinsex",
        "http://www.sexinsex.net/bbs/",
        "forum-%d-%d.html",
        SexInSex_Login(save_root_path),
        "gbk",
        [383, 322, 359, 390],
    ],
]
bbsdef_ids = [x[1] for x in bbsdef]


### 获取html的爬虫类
class Crawler:
    @classmethod
    def getCrawler(cls, bbsId):
        if not hasattr(cls, "crawlers"):
            cls.crawlers = {}

        if isinstance(bbsId, int):
            bbsinfo = bbsdef[bbsId]
        elif isinstance(bbsId, str):
            for s in bbsdef:
                if s[1] == bbsId:
                    bbsinfo = s
                    break
        if not bbsinfo:
            raise "There is UNKNOWN bbsinfo."

        siteId = bbsinfo[1]
        if not siteId in cls.crawlers:
            login = bbsinfo[4]
            obj = cls(1, login)
            (
                obj.bbsname,
                obj.siteId,
                obj.base,
                obj.index_page,
                _,
                obj.encoding,
                obj.boardIds,
            ) = bbsinfo
            cls.crawlers[siteId] = obj
        return cls.crawlers[siteId]

    def __init__(self, delay, login):
        self.head = {
            "User-Agent": "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.94 Safari/537.36"
        }

        self.proxy = {
            "http": "http://127.0.0.1:8080",
            "https": "https://127.0.0.1:8080",
        }

        self.delay = delay
        self.last_accessed = 0

        self.session = requests.Session()
        if login:
            login.login(self.session, self.head, self.proxy)

    def getUrl(self, url):
        if self.delay > 0:
            sleep_secs = self.delay - (time.time() - self.last_accessed)

            if sleep_secs > 0:
                time.sleep(sleep_secs)
        self.last_accessed = time.time()

        url = self.base + url
        response = self.session.get(url, headers=self.head, proxies=self.proxy)

        html = response.content.decode(self.encoding, "ignore")
        print("Get [%s] OK" % url)
        return html


### Thread对象
def MakeThread(siteId, threadId, title, author, postTime, link):
    postTime = time.strftime("%Y-%m-%d", time.strptime(postTime, "%Y-%m-%d"))
    dic = {
        "siteId": siteId,
        "threadId": threadId,
        "title": title,
        "author": author,
        "postTime": postTime,
        "link": link,
    }
    return dic


class MetaData:
    """docstring for MetaData"""

    def __init__(self, save_root_path):
        self.meta_data_path = os.path.join(save_root_path, "meta")
        self._load_meta_data()

    def _load_meta_data(self):
        ### 如果存在json，load数据
        if os.path.exists(self.meta_data_path):
            timestamp_json = os.path.join(self.meta_data_path, "timestamp.json")
            with open(timestamp_json, encoding="utf-8") as f:
                self.last_timestamp = json.load(f)

            self.last_threads = load_threads(self.meta_data_path)

            self.tags = load_tags(self.meta_data_path)

            superkeywords_json = os.path.join(self.meta_data_path, "superkeywords.json")
            with open(superkeywords_json, encoding="utf-8") as f:
                self.superkeywords = json.load(f)

            blacklist_json = os.path.join(self.meta_data_path, "blacklist.json")
            with open(blacklist_json, encoding="utf-8") as f:
                self.blacklist = json.load(f)

            self.manual_topics = load_manual_topics(self.meta_data_path)

        ### 如果不存在json，初始化空数据
        else:
            self.last_timestamp = 0
            self.last_threads = []
            self.tags = {}
            self.superkeywords = []
            self.blacklist = []
            self.manual_topics = {}

    def merge_threads(self, latest_threads, force=False):
        # 临时存储Manual SK tids
        manual_threads = {
            i: [self.last_threads[y] for y in x["tids"]]
            for (i, x) in enumerate(self.superkeywords)
            if x["skType"] == SK_Type.Manual
        }

        ### 合并新旧数据
        def merge(lasts, latest, force):
            threads = lasts[:]

            lastIds = [(x["siteId"], x["threadId"]) for x in lasts]
            for t in latest:
                if not force:
                    if not (t["siteId"], t["threadId"]) in lastIds:
                        threads.append(t)
                else:
                    if (t["siteId"], t["threadId"]) in lastIds:
                        idx = lastIds.index((t["siteId"], t["threadId"]))
                        threads.pop(idx)
                        threads.insert(idx, t)
                    else:
                        threads.append(t)
            return threads

        threads = merge(self.last_threads, latest_threads, force)
        thread_ids = [(x["siteId"], x["threadId"]) for x in threads]
        # 更新新的tids
        for i, x in manual_threads.items():
            tids = [thread_ids.index((t["siteId"], t["threadId"])) for t in x]
            self.superkeywords[i]["tids"] = tids

        ### 扫描数据，重新提取关键字
        favorites = [
            x["keyword"] for x in self.superkeywords if x["skType"] == SK_Type.Simple
        ]

        for superkeyword in self.superkeywords:
            if superkeyword["skType"] != SK_Type.Manual:
                superkeyword["tids"] = []
                superkeyword["kws"] = []

        def find_keywords(title):
            keywords = [title]
            keywords += [re.sub("\\(.*?\\)", "", title)]
            keywords += [re.sub("（.*?\\)", "", title)]
            keywords += [re.sub("\\(.*?）", "", title)]
            keywords += [re.sub("（.*?）", "", title)]
            keywords += [re.sub("【.*?】", "", title)]
            keywords = list(set(keywords))
            keywords += re.findall("【(.*?)】", title)
            keywords += re.findall("【(.*?)番外篇.*?】", title)
            keywords += re.findall("【(.*?)第?[ 0-9\\-]+[部章卷篇].*?】", title)
            keywords += re.findall("【(.*?)第?[ 0-9\\-]+[部章卷篇].*?】", title)
            keywords += re.findall("【(.*?)第?[零一二三四五六七八九十百千万]+[部章卷篇].*?】", title)
            keywords_no_kh = [
                re.sub("(?:（|\\()(.*?)(?:\\)|）)", "", x)
                for x in keywords
                if re.search("(?:（|\\()(.*?)(?:\\)|）)", x)
            ]
            keywords_in_kh = []
            for x in keywords:
                if re.search("(?:（|\\()(.*?)(?:\\)|）)", x):
                    keywords_in_kh += re.findall("(?:（|\\()(.*?)(?:\\)|）)", x)
                    keywords_in_kh += re.findall("(?:（|『)(.*?)(?:』|）)", x)
                    keywords_in_kh += re.findall("(?:（|「)(.*?)(?:」|）)", x)
            keywords += keywords_no_kh
            keywords += keywords_in_kh
            keywords = [x for x in keywords if not re.match("^【.*?】$", x)]
            keywords = [x for x in keywords if not re.match("^\\(.*?\\)$", x)]
            keywords = [x for x in keywords if not re.match("^（.*?\\)$", x)]
            keywords = [x for x in keywords if not re.match("^\\(.*?）$", x)]
            keywords = [x for x in keywords if not re.match("^（.*?）$", x)]
            keywords = list(set(keywords))
            keywords = [x for x in keywords if not re.match("^[ 　]+$", x)]
            keywords = [x for x in keywords if not re.match("^第?[ 0-9\\-]+[部章卷篇]?$", x)]
            keywords = [
                x for x in keywords if not re.match("^第?[ ０１２３４５６７８９]+[部章卷篇]?$", x)
            ]
            keywords = [
                x for x in keywords if not re.match("^第?[零一二三四五六七八九十百千万]+[部章卷篇]?$", x)
            ]
            keywords = [x for x in keywords if not re.match("^[上中下终][章卷篇]?$", x)]
            keywords = [x for x in keywords if not re.match("^[续完]$", x)]
            keywords = [x for x in keywords if not re.match("^大?结局$", x)]
            keywords = [x for x in keywords if not re.match("^代友?发$", x)]
            keywords = sum([[x] + x.split("：") for x in keywords], [])
            keywords = list(set(keywords))
            keywords = [x.strip() for x in keywords]
            return keywords

        def find_sub_keywords(title, sub_keywords):
            res = []
            for j, s_kws in enumerate(sub_keywords):
                for s_kw in s_kws:
                    if s_kw in title:
                        res.append(j)
                        break
            return tuple(res)

        tags = {}
        for i, t in enumerate(threads):
            if t["siteId"] not in bbsdef_ids:
                continue
            title = t["title"]
            author = t["author"]
            keywords = find_keywords(title)
            for keyword in keywords:
                if (
                    keyword != ""
                    and not keyword.isdigit()
                    and not keyword in self.blacklist
                    and not keyword in favorites
                ):
                    if not keyword in tags:
                        tags[keyword] = []
                    tags[keyword].append(i)

            for superkeyword in self.superkeywords:
                keyword = superkeyword["keyword"]
                authors = superkeyword["author"]
                aliases = superkeyword["alias"]
                sub_keywords = superkeyword["subKeywords"]
                if superkeyword["skType"] == SK_Type.Simple:
                    if keyword in keywords:
                        superkeyword["tids"].append(i)
                        superkeyword["kws"].append(())
                    for alias in aliases:
                        if alias in keywords:
                            superkeyword["tids"].append(i)
                            superkeyword["kws"].append(())
                elif superkeyword["skType"] == SK_Type.Advanced:
                    if keyword in title:
                        if authors[0] == "*" or authors.count(author) > 0:
                            superkeyword["tids"].append(i)
                            superkeyword["kws"].append(())
                elif superkeyword["skType"] == SK_Type.Author:
                    if authors.count(author) > 0:
                        superkeyword["tids"].append(i)
                        kws = find_sub_keywords(title, sub_keywords)
                        superkeyword["kws"].append(kws)
                elif superkeyword["skType"] == SK_Type.Manual:
                    pass
                else:
                    pass

        for superkeyword in self.superkeywords:
            dup_ids = []
            keys = []
            for i, j in enumerate(superkeyword["tids"]):
                t = threads[j]
                key = t["siteId"] + "/" + t["threadId"]
                if key not in keys:
                    keys.append(key)
                else:
                    dup_ids.append(i)

            for i in reversed(dup_ids):
                del superkeyword['tids'][i]
                del superkeyword['kws'][i]

        ### 主题下，按照时间排序
        def getPostTime(x):
            timestr = threads[x]["postTime"]
            secs = time.mktime(time.strptime(timestr, "%Y-%m-%d"))
            return secs

        for keyword in tags:
            tags[keyword].sort(
                key=lambda x: (getPostTime(x), threads[x]["threadId"]), reverse=True
            )

        for superkeyword in self.superkeywords:
            if superkeyword["skType"] != SK_Type.Manual:
                tids_kws = list(zip(superkeyword["tids"], superkeyword["kws"]))
                tids_kws = unique_tuple(tids_kws, key=lambda x: x[0])
                tids_kws.sort(
                    key=lambda x: (getPostTime(x[0]), threads[x[0]]["threadId"]),
                    reverse=True,
                )
                tids, kws = zip(*tids_kws)
                superkeyword["tids"] = tids
                superkeyword["kws"] = kws

        self.tags = tags
        self.last_threads = threads
        print("tags: %d" % len(tags))

    def save_meta_data(self):
        os.makedirs(self.meta_data_path, exist_ok=True)

        timestamp_json = os.path.join(self.meta_data_path, "timestamp.json")
        with open(timestamp_json, "w", encoding="utf-8") as f:
            json.dump(self.last_timestamp, f)

        save_threads(self.last_threads, self.meta_data_path)

        save_tags(self.tags, self.meta_data_path)

        superkeywords_json = os.path.join(self.meta_data_path, "superkeywords.json")
        with open(superkeywords_json, "w", encoding="utf-8") as f:
            json.dump(self.superkeywords, f)

        blacklist_json = os.path.join(self.meta_data_path, "blacklist.json")
        with open(blacklist_json, "w", encoding="utf-8") as f:
            json.dump(self.blacklist, f)

        save_manual_topcis(self.manual_topics, self.meta_data_path)

    def load_mts(self, siteId):
        return _load_mts(siteId, self.superkeywords, self.manual_topics)

    def find_mt(self, sk):
        return _find_mt(sk, self.manual_topics)


def keytext(superkeyword):
    if superkeyword["skType"] == SK_Type.Simple:
        return superkeyword["keyword"]
    else:
        return superkeyword["author"][0] + ":" + superkeyword["keyword"]


def unique_tuple(l, key):
    key_helper = []
    l2 = []
    for t in l:
        k = key(t)
        if k not in key_helper:
            key_helper.append(k)
            l2.append(t)
    return l2


from urllib.parse import urlparse, parse_qsl, unquote_plus


def url_equals(u1, u2):
    def __parse_parts(url):
        parts = urlparse(url)
        _query = frozenset(parse_qsl(parts.query))
        _path = unquote_plus(parts.path)
        parts = parts._replace(scheme="https", query=_query, path=_path)
        return parts

    parts1 = __parse_parts(u1)
    parts2 = __parse_parts(u2)
    return parts1 == parts2


def url_in(u, urls):
    for url in urls:
        if url_equals(u, url):
            return True
    return False
