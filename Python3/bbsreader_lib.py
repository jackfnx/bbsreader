import os
import re
import yaml
import json
import time
import requests

save_root_path = 'C:/Users/hpjing/Dropbox/BBSReader.Cache'


class SexInSex_Login:
    def __init__(self, save_root_path):
        passwd_yaml = os.path.join(save_root_path, 'PASSWORD.yaml')
        with open(passwd_yaml) as f:
            passwd = yaml.load(f)
            self.name = passwd['SexInSex']['name']
            self.pw = passwd['SexInSex']['pw']

    def login(self, session, head, proxy):
        loginurl = 'http://www.sexinsex.net/bbs/logging.php?action=login&'
        loginparams = {
            'formhash': 'f33adb50',
            'referer': 'http://www.sexinsex.net/bbs/forum-372-1.html',
            'loginfield': 'username',
            'username': self.name,
            'password': self.pw,
            'questionid': 0,
            'anwser': '',
            'cookietime': 315360000,
            'loginmode': 'normal',
            'styleid': 0,
            'loginsubmit': True
        }

        resp = session.post(loginurl, data=loginparams, headers=head, proxies=proxy)
        print('[色中色] 登陆: %s' % (resp))


### BBS列表
bbsdef = [
    ['第一会所', 'sis001', 'http://www.sis001.com/forum/', 'forum-%d-%d.html', None, [383,322]],
    ['色中色', 'sexinsex', 'http://www.sexinsex.net/bbs/', 'forum-%d-%d.html', SexInSex_Login(save_root_path), [383,322,359]],
]



### 获取html的爬虫类
class Crawler:

    @classmethod
    def getCrawler(cls, bbsId):
        if not hasattr(cls, 'crawlers'):
            cls.crawlers = {}

        if isinstance(bbsId, int):
            bbsinfo = bbsdef[bbsId]
        elif isinstance(bbsId, str) or isinstance(bbsId, unicode):
            for s in bbsdef:
                if s[1] == bbsId:
                    bbsinfo = s
                    break
        if not bbsinfo:
            raise 'There is UNKNOWN bbsinfo.'

        siteId = bbsinfo[1]
        if not siteId in cls.crawlers:
            login = bbsinfo[4]
            obj = cls(1, login)
            obj.bbsname, obj.siteId, obj.base, obj.index_page, _, obj.boardIds = bbsinfo
            cls.crawlers[siteId] = obj
        return cls.crawlers[siteId]

    def __init__(self, delay, login):
        
        self.head = {
            'User-Agent': 'Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.94 Safari/537.36'
        }

        self.proxy = {
            "http": "http://127.0.0.1:8080",
            "https": "https://127.0.0.1:8080"
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

        html = response.content.decode('gbk', 'ignore')
        print('Get [%s] OK' % url)
        return html



### Thread对象
def MakeThread(siteId, threadId, title, author, postTime, link):
    dic = {
        'siteId': siteId,
        'threadId': threadId,
        'title': title,
        'author': author,
        'postTime': postTime,
        'link': link
    }
    return dic


class MetaData:
    """docstring for MetaData"""
    def __init__(self, save_root_path):
        self.meta_data_path = os.path.join(save_root_path, 'meta.json')
        self._load_meta_data()


    def _load_meta_data(self):

        ### 如果存在json，load数据
        if os.path.exists(self.meta_data_path):
            with open(self.meta_data_path, encoding='utf-8') as f:
                load_data = json.load(f)
            self.last_timestamp = load_data['timestamp']
            self.last_threads = load_data['threads']
            self.tags = load_data['tags']
            self.superkeywords = load_data['superkeywords']
            self.blacklist = load_data['blacklist']
        ### 如果不存在json，初始化空数据
        else:
            self.last_timestamp = 0
            self.last_threads = []
            self.tags = {}
            self.superkeywords = []
            self.blacklist = []


    def merge_threads(self, latest_threads, force=False):

        ### 合并新旧数据
        def merge(lasts, latest, force):
            threads = lasts[:]
            
            lastIds = [(x['siteId'], x['threadId']) for x in lasts]
            for t in latest:
                if not force:
                    if not (t['siteId'], t['threadId']) in lastIds:
                        threads.append(t)
                else:
                    if (t['siteId'], t['threadId']) in lastIds:
                        threads.pop(lastIds.index((t['siteId'], t['threadId'])))
                    threads.append(t)
            return threads

        threads = merge(self.last_threads, latest_threads, force)


        ### 扫描数据，重新提取关键字
        favorites = [x['keyword'] for x in self.superkeywords if x['simple']]

        for superkeyword in self.superkeywords:
            superkeyword['tids'] = []


        def find_keywords(title):
            keywords = [title]
            keywords += [re.sub('\\(.*?\\)', '', title)]
            keywords += [re.sub('（.*?\\)', '', title)]
            keywords += [re.sub('\\(.*?）', '', title)]
            keywords += [re.sub('（.*?）', '', title)]
            keywords += [re.sub('【.*?】', '', title)]
            keywords = list(set(keywords))
            keywords += re.findall('【(.*?)】', title)
            keywords_no_kh = [re.sub('(?:（|\\()(.*?)(?:\\)|）)', '', x) for x in keywords if re.search('(?:（|\\()(.*?)(?:\\)|）)', x)]
            keywords_in_kh = []
            for x in keywords:
                if re.search('(?:（|\\()(.*?)(?:\\)|）)', x):
                    keywords_in_kh += re.findall('(?:（|\\()(.*?)(?:\\)|）)', x)
            keywords += keywords_no_kh
            keywords += keywords_in_kh
            keywords = [x for x in keywords if not re.match('^【.*?】$', x)]
            keywords = [x for x in keywords if not re.match('^\\(.*?\\)$', x)]
            keywords = [x for x in keywords if not re.match('^（.*?\\)$', x)]
            keywords = [x for x in keywords if not re.match('^\\(.*?）$', x)]
            keywords = [x for x in keywords if not re.match('^（.*?）$', x)]
            keywords = list(set(keywords))
            keywords = [x for x in keywords if not re.match('^[ 　]+$', x)]
            keywords = [x for x in keywords if not re.match('^第?[ 0-9\\-]+[章卷篇]?$', x)]
            keywords = [x for x in keywords if not re.match('^第?[ ０１２３４５６７８９]+[章卷篇]?$', x)]
            keywords = [x for x in keywords if not re.match('^第?[零一二三四五六七八九十百千万]+[章卷篇]?$', x)]
            keywords = [x for x in keywords if not re.match('^[上中下终][章卷篇]?$', x)]
            keywords = [x for x in keywords if not re.match('^[续完]$', x)]
            keywords = [x for x in keywords if not re.match('^大?结局$', x)]
            keywords = [x for x in keywords if not re.match('^代友?发$', x)]
            keywords = [x.strip() for x in keywords]
            return keywords


        tags = {}
        for i in range(len(threads)):
            t = threads[i]
            title = t['title']
            author = t['author']
            keywords = find_keywords(title)
            for keyword in keywords:
                if keyword != '' and not keyword.isdigit() and not keyword in self.blacklist and not keyword in favorites:
                    if not keyword in tags:
                        tags[keyword] = []
                    tags[keyword].append(i)

            for superkeyword in self.superkeywords:
                keyword = superkeyword['keyword']
                authors = superkeyword['author']
                if superkeyword['simple']:
                    if keyword in keywords:
                        superkeyword['tids'].append(i)
                else:
                    if keyword == '*' or keyword in title:
                        if authors[0] == '*' or authors.count(author) > 0:
                            superkeyword['tids'].append(i)


        ### 主题下，按照时间排序
        def getPostTime(x):
            timestr = threads[x]['postTime']
            secs = time.mktime(time.strptime(timestr, '%Y-%m-%d'))
            return secs

        for keyword in tags:
            tags[keyword].sort(key=lambda x: (getPostTime(x), threads[x]['threadId']), reverse=True)

        for superkeyword in self.superkeywords:
            tids = superkeyword['tids']
            tids = list(set(tids))
            tids.sort(key=lambda x: (getPostTime(x), threads[x]['threadId']), reverse=True)
            superkeyword['tids'] = tids

        self.tags = tags
        self.last_threads = threads
        print('tags: %d' % len(tags))


    def save_meta_data(self):

        ### 保存data
        with open(self.meta_data_path, 'w', encoding='utf-8') as f:
            save_data = {
                'timestamp': time.time(),
                'threads': self.last_threads,
                'tags': self.tags,
                'superkeywords': self.superkeywords,
                'blacklist': self.blacklist
            }
            json.dump(save_data, f)

