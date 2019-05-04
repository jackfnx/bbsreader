import os
import yaml
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
