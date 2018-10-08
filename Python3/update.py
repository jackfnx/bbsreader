#!/usr/bin/env python
# coding: utf-8
import os
import sys
import time
import json
import re
import requests
from bs4 import BeautifulSoup
import argparse

from login_sexinsex import SexInSex_Login

save_root_path = 'C:/Users/hpjing/Dropbox/BBSReader.Cache'

### BBS列表
bbsdef = [
    ['第一会所', 'sis001', 'http://www.sis001.com/forum/', 'forum-%d-%d.html', None, [383,322]],
    ['色中色', 'sexinsex', 'http://www.sexinsex.net/bbs/', 'forum-%d-%d.html', SexInSex_Login(save_root_path), [383,359]],
]


### 参数
parser = argparse.ArgumentParser()
parser.add_argument('pages', nargs='?', type=int, help='manual set download pages number')
parser.add_argument('-l', '--list', action='store_true', help='support BBS list')
parser.add_argument('--bbsid', type=int, default=0, help='manual set <BBS ID>')
parser.add_argument('--boardid', type=int, default=None, help='manual set <BOARD ID>')
args = parser.parse_args()

pages = args.pages
bbsId = args.bbsid
boardId = args.boardid

if args.list:
    print('%s\n' % '\n'.join([str(x) for x in bbsdef]))
    sys.exit(0)

meta_data_path = os.path.join(save_root_path, 'meta.json')

### 如果存在json，load数据
if os.path.exists(meta_data_path):
    with open(meta_data_path, encoding='utf-8') as f:
        load_data = json.load(f)
    last_timestamp = load_data['timestamp']
    last_threads = load_data['threads']
    tags = load_data['tags']
    anthologies = load_data['anthologies']
    favorites = load_data['favorites']
    blacklist = load_data['blacklist']
    followings = load_data['followings']
    tag_groups = load_data['tag_groups']
    anthology_groups = load_data['anthology_groups']
### 如果不存在json，初始化空数据
else:
    last_timestamp = 0
    last_threads = []
    tags = {}
    anthologies = {}
    favorites = ['琼明神女录', '淫堕的女武神']
    blacklist = []
    followings = {}
    tag_groups = {}
    anthology_groups = {}


### 根据上次更新时间，确定这次读取几页
if pages is None:
    now = time.time()
    if last_timestamp == 0:
        pages = 100
    elif now - last_timestamp < 60*60*24*10: # 没超过10天，查看5页
        pages = 5
    else:
        pages = 30
    tmstr = time.strftime('%Y-%m-%d %H:%M:%S', time.localtime(last_timestamp))
    print('] last update at [%s], this update will load <%d> pages' % (tmstr, pages))
else:
    print('] manual set, update <%d> pages.' % (pages))



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


crawler = Crawler.getCrawler(bbsId)


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

### 读取版面
def bbsdoc(html, siteId):
    soup = BeautifulSoup(html, 'html5lib')
    objs = soup.select('tbody[id^=stickthread_]')
    objs += soup.select('tbody[id^=normalthread_]')
    threads = []
    for t in objs:
        title = t.select('th span[id^=thread_] a')[0].text
        link = t.select('th span[id^=thread_] a')[0]['href']
        threadId = link.split('-')[1]
        author = t.select('td.author cite a')[0].text
        postTime = t.select('td.author em')[0].text
        threads.append(MakeThread(siteId, threadId, title, author, postTime, link))
    return threads

### 读取文章
def bbscon(html):
    soup = BeautifulSoup(html, 'html5lib')
    postobj = soup.select('div[id^=postmessage_] div[id^=postmessage_]')[0]
    [x.decompose() for x in postobj.select('strong')]
    [x.decompose() for x in postobj.select('table')]
    return postobj.text



### 读取新数据
def update_threads(boardId, threads):
    print('Updating [%s] <board: %d>.' % (crawler.bbsname, boardId))
    for i in range(0, pages):
        url = crawler.index_page % (boardId, (i+1))
        try:
            threads += bbsdoc(crawler.getUrl(url), crawler.siteId)
        except Exception as e:
            print('Get [%s]: %s' % (url, str(e)))


latest_threads = []
if boardId is None:
    for curr_board in crawler.boardIds:
        update_threads(curr_board, latest_threads)
else:
    curr_board = crawler.boardIds[boardId]
    update_threads(curr_board, latest_threads)


### 合并新旧数据
def merge(lasts, latest):
    threads = lasts[:]
    
    lastIds = [(x['siteId'], x['threadId']) for x in lasts]
    for t in latest:
        if not (t['siteId'], t['threadId']) in lastIds:
            threads.append(t)
    return threads

threads = merge(last_threads, latest_threads)



### 扫描数据，提取关键字
tags = {}
anthologies = [0] * len(followings)
for i in range(len(threads)):
    t = threads[i]
    title = t['title']
    author = t['author']
    keywords = re.findall('【(.*?)】', title)
    for keyword in keywords:
        if not keyword.isdigit() and not keyword in blacklist:
            if not keyword in tags:
                tags[keyword] = []
            tags[keyword].append(i)

    for j in range(len(followings)):
        superkeyword = followings[j]
        keyword = superkeyword['keyword']
        authors = superkeyword['author']
        if keyword == '*' or keyword in title:
            if authors[0] == '*' or author in authors:
                if not isinstance(anthologies[j], list):
                    anthologies[j] = []
                anthologies[j].append(i)


### 主题下，按照时间排序
def getPostTime(x):
    timestr = threads[x]['postTime']
    secs = time.mktime(time.strptime(timestr, '%Y-%m-%d'))
    return secs

for keyword in tags:
    tags[keyword].sort(key=lambda x: (getPostTime(x), threads[x]['threadId']), reverse=True)

for i in range(len(anthologies)):
    anthologies[i] = list(set(anthologies[i]))
    anthologies[i].sort(key=lambda x: (getPostTime(x), threads[x]['threadId']), reverse=True)


### 根据收藏夹，扫描所有文章，是否存在本地数据，如果不存在则下载
def download_article(t):
    crawler = Crawler.getCrawler(t['siteId'])

    save_path = os.path.join(save_root_path, t['siteId'])

    txtpath = os.path.join(save_root_path, t['siteId'], t['threadId'] + '.txt')
    if not os.path.exists(txtpath):
        if not os.path.isdir(save_path):
            os.makedirs(save_path)
        try:
            chapter = bbscon(crawler.getUrl(t['link']))
            with open(txtpath, 'w', encoding='utf-8') as f:
                f.write(chapter)
        except Exception as e:
            print('Get [%s]: %s' % (t['link'], str(e)))

for tag in favorites:
    if tag in tags:
        for i in tags[tag]:
            t = threads[i]
            download_article(t)
        print('keyword [%s] saved.' % tag)

for i in range(len(followings)):
    superkeyword = followings[i]
    anthology = anthologies[i]
    key = superkeyword['author'][0] + ":" + superkeyword['keyword']
    for j in anthology:
        t = threads[j]
        download_article(t)
    print('anthology [%s] saved.' % key)


## 保存data
with open(meta_data_path, 'w', encoding='utf-8') as f:
    save_data = {
        'timestamp': time.time(),
        'threads': threads,
        'tags': tags,
        'anthologies': anthologies,
        'favorites': favorites,
        'blacklist': blacklist,
        'followings': followings,
        'tag_groups': tag_groups,
        'anthology_groups': anthology_groups,
    }
    json.dump(save_data, f)





