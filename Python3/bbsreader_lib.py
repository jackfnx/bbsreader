import os
import re
import yaml
import json
import time
import requests
import pinyin


save_root_path = 'C:/Users/hpjing/Dropbox/BBSReader.Cache'

class SexInSex_Login:
    def __init__(self, save_root_path):
        passwd_yaml = os.path.join(save_root_path, 'PASSWORD.yaml')
        with open(passwd_yaml) as f:
            passwd = yaml.load(f, Loader=yaml.FullLoader)
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
    ['第一会所', 'sis001', 'http://www.sis001.com/forum/', 'forum-%d-%d.html', None, 'utf-8', [383,322]],
    ['色中色', 'sexinsex', 'http://www.sexinsex.net/bbs/', 'forum-%d-%d.html', SexInSex_Login(save_root_path), 'gbk', [383,322,359]],
]

### 获取html的爬虫类
class Crawler:

    @classmethod
    def getCrawler(cls, bbsId):
        if not hasattr(cls, 'crawlers'):
            cls.crawlers = {}

        if isinstance(bbsId, int):
            bbsinfo = bbsdef[bbsId]
        elif isinstance(bbsId, str):
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
            obj.bbsname, obj.siteId, obj.base, obj.index_page, _, obj.encoding, obj.boardIds = bbsinfo
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

        html = response.content.decode(self.encoding, 'ignore')
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
        self.meta_data_path = os.path.join(save_root_path, 'meta')
        self._load_meta_data()


    def _load_meta_data(self):

        ### 如果存在json，load数据
        if os.path.exists(self.meta_data_path):
            timestamp_json = os.path.join(self.meta_data_path, 'timestamp.json')
            with open(timestamp_json, encoding='utf-8') as f:
                self.last_timestamp = json.load(f)

            self.last_threads = []
            threads_dir = os.path.join(self.meta_data_path, 'threads')
            threads_fs = sorted(os.listdir(threads_dir), key=lambda x: int(x.split('-')[0]))
            for threads_fname in threads_fs:
                threads_json = os.path.join(threads_dir, threads_fname)
                with open(threads_json, encoding='utf-8') as f:
                    self.last_threads += json.load(f)

            self.tags = {}
            tags_dir = os.path.join(self.meta_data_path, 'tags')
            for tags_fname in os.listdir(tags_dir):
                tags_json = os.path.join(tags_dir, tags_fname)
                with open(tags_json, encoding='utf-8') as f:
                    tags_segs = json.load(f)
                self.tags.update(tags_segs)

            superkeywords_json = os.path.join(self.meta_data_path, 'superkeywords.json')
            with open(superkeywords_json, encoding='utf-8') as f:
                self.superkeywords = json.load(f)

            blacklist_json = os.path.join(self.meta_data_path, 'blacklist.json')
            with open(blacklist_json, encoding='utf-8') as f:
                self.blacklist = json.load(f)
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
        def get_pinyin_first_alpha(name, n=1):
            def Q2B(uchar):
                inside_code = ord(uchar)
                if inside_code == 12288:  # 全角空格直接转换
                    inside_code = 32
                elif (inside_code >= 65281 and inside_code <= 65374):  # 全角字符（除空格）根据关系转化
                    inside_code -= 65248
                return chr(inside_code)
            name = ''.join(Q2B(e) for e in name if e.isalnum())
            pre = pinyin.get_initial(name).replace(' ', '')
            if len(pre) < n:
                pre = '_'
            else:
                pre = pre[:n].upper()
            return pre

        if not os.path.exists(self.meta_data_path):
            os.makedirs(self.meta_data_path)

        timestamp_json = os.path.join(self.meta_data_path, 'timestamp.json')
        with open(timestamp_json, 'w', encoding='utf-8') as f:
            json.dump(self.last_timestamp, f)

        threads_dir = os.path.join(self.meta_data_path, 'threads')
        if not os.path.exists(threads_dir):
            os.makedirs(threads_dir)

        threads = {}
        for i in range(0, len(self.last_threads), 1000):
            threads_fname = '%d-%s-x.json' % (i, self.last_threads[i]['threadId'])
            threads[threads_fname] = self.last_threads[i:i+1000]

        threads_rm = [os.path.join(threads_dir, x) for x in os.listdir(threads_dir) if x not in threads]
        for t in threads_rm:
            os.unlink(t)

        for threads_fname in threads:
            threads_json_s = json.dumps(threads[threads_fname], indent=2)
            threads_json = os.path.join(threads_dir, threads_fname)
            if os.path.exists(threads_json):
                with open(threads_json, encoding='utf-8') as f:
                    s = f.read()
            else:
                s = ''
            if s != threads_json_s:
                with open(threads_json, 'w', encoding='utf-8') as f:
                    f.write(threads_json_s)

        tags_dir = os.path.join(self.meta_data_path, 'tags')
        if not os.path.exists(tags_dir):
            os.makedirs(tags_dir)

        tags = {}
        for t, tv in self.tags.items():
            tags_fname = get_pinyin_first_alpha(t, n=1) + '.json'
            if tags_fname not in tags:
                tags[tags_fname] = {}
            tags[tags_fname][t] = tv

        tags_rm = [os.path.join(tags_dir, x) for x in os.listdir(tags_dir) if x not in tags]
        for t in tags_rm:
            os.unlink(t)

        for tags_fname in tags:
            tags_json_s = json.dumps(tags[tags_fname], indent=2)
            tags_json = os.path.join(tags_dir, tags_fname)
            if os.path.exists(tags_json):
                with open(tags_json, encoding='utf-8') as f:
                    s = f.read()
            else:
                s = ''
            if s != tags_json_s:
                with open(tags_json, 'w', encoding='utf-8') as f:
                    f.write(tags_json_s)

        superkeywords_json = os.path.join(self.meta_data_path, 'superkeywords.json')
        with open(superkeywords_json, 'w', encoding='utf-8') as f:
            json.dump(self.superkeywords, f)

        blacklist_json = os.path.join(self.meta_data_path, 'blacklist.json')
        with open(blacklist_json, 'w', encoding='utf-8') as f:
            json.dump(self.blacklist, f)


