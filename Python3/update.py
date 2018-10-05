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
    ['色中色', 'sexinsex', 'http://www.sexinsex.net/bbs/', 'forum-%d-%d.html', SexInSex_Login(save_root_path), [383,]],
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

bbsname, folder, base, start_path, login, boardIds = bbsdef[bbsId]

save_path = os.path.join(save_root_path, folder)
if not os.path.isdir(save_path):
    os.makedirs(save_path)

meta_data_path = os.path.join(save_path, 'meta.json')

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
### 如果不存在json，初始化空数据
else:
    last_timestamp = 0
    last_threads = []
    tags = {}
    anthologies = {}
    favorites = ['琼明神女录', '册母为后', '绿帽武林之淫乱后宫', '龙珠肏', '锦绣江山传', '淫堕的女武神']
    blacklist = []
    followings = {}


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
        
        response = self.session.get(url, headers=self.head, proxies=self.proxy)

        html = response.content.decode('gbk', 'ignore')
        print('Get [%s] OK' % url)
        return html



crawler = Crawler(1, login)


### Thread对象
def MakeThread(threadId, title, author, postTime, link):
    dic = {
        'threadId': threadId,
        'title': title,
        'author': author,
        'postTime': postTime,
        'link': link
    }
    return dic

### 读取版面
def bbsdoc(html):
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
        threads.append(MakeThread(threadId, title, author, postTime, link))
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
    print('Updating [%s] <board: %d>.' % (bbsname, boardId))
    for i in range(0, pages):
        url = base + (start_path % (boardId, (i+1)))
        try:
            threads += bbsdoc(crawler.getUrl(url))
        except Exception as e:
            print('Get [%s]: %s' % (url, str(e)))


latest_threads = []
if boardId is None:
    for curr_board in boardIds:
        update_threads(curr_board, latest_threads)
else:
    curr_board = boardIds[boardId]
    update_threads(curr_board, latest_threads)


### 合并新旧数据
def merge(lasts, latest):
    threads = lasts[:]
    
    lastIds = [x['threadId'] for x in lasts]
    for t in latest:
        if not t['threadId'] in lastIds:
            threads.append(t)
    return threads

threads = merge(last_threads, latest_threads)



tags = {}
### 扫描数据，提取关键字
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

    if author in followings:
        koi = followings[author]
        if koi == '*' or koi in title:
            key = author + ':' + koi
            if not key in anthologies:
                anthologies[key] = []
            anthologies[key].append(i)


### 主题下，按照时间排序
for keyword in tags:
    tags[keyword].sort(key=lambda x: int(threads[x]['threadId']), reverse=True)

for key in anthologies:
    anthologies[key] = list(set(anthologies[key]))
    anthologies[key].sort(key=lambda x: int(threads[x]['threadId']), reverse=True)

### 根据收藏夹，扫描所有文章，是否存在本地数据
def download_article(t):
    txtpath = os.path.join(save_path, t['threadId'] + '.txt')
    if not os.path.exists(txtpath):
        url = base + t['link']
        try:
            chapter = bbscon(crawler.getUrl(url))
            with open(txtpath, 'w', encoding='utf-8') as f:
                f.write(chapter)
        except Exception as e:
            print('Get [%s]: %s' % (url, str(e)))

for tag in favorites:
    if tag in tags:
        for i in tags[tag]:
            t = threads[i]
            download_article(t)
        print('keyword [%s] saved.' % tag)

for author in followings:
    koi = followings[author]
    key = author + ':' + koi
    if key in anthologies:
        for i in anthologies[key]:
            t = threads[i]
            download_article(t)
        print('anthology [%s] saved.' % key)



### 保存data
with open(meta_data_path, 'w', encoding='utf-8') as f:
    save_data = {
        'timestamp': time.time(),
        'threads': threads,
        'tags': tags,
        'anthologies': anthologies,
        'favorites': favorites,
        'blacklist': blacklist,
        'followings': followings,
    }
    json.dump(save_data, f)





