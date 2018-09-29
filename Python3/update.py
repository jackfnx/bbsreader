#!/usr/bin/env python
# coding: utf-8
import os
import sys
import time
import json
import re
from urllib import request
from bs4 import BeautifulSoup
import argparse

save_root_path = 'E:/BBSReader.Cache'

### BBS列表
bbsdef = [
    ['第一会所', 'sis001', 'http://www.sis001.com/forum/', 'forum-%d-%d.html', [383,322]],
    ['色中色', 'sexinsex', 'http://www.sexinsex.net/forum/', 'forum-%d-%d.html', [383,]],
]

### 参数
parser = argparse.ArgumentParser()
parser.add_argument('pages', nargs='?', type=int, help='manual set download pages number')
parser.add_argument('-l', '--list', action='store_true', help='support BBS list')
parser.add_argument('--bbsid', type=int, default=0, help='manual set <BBS ID>')
parser.add_argument('--boardid', type=int, default=0, help='manual set <BOARD ID>')
args = parser.parse_args()

pages = args.pages
bbsId = args.bbsid
boardId = args.boardid

if args.list:
    print('%s\n' % '\n'.join([str(x) for x in bbsdef]))
    sys.exit(0)

# if len(sys.argv) < 3:
#     sys.stderr.write('%s\n' % '\n'.join([str(x) for x in bbsdef]))
#     sys.exit(0)
    
# bbsId = int(sys.argv[1])
# boardId = int(sys.argv[2])
# bbsId = 0
# boardId = 0
# pages = int(sys.argv[1]) if len(sys.argv) > 1 else None

bbsname, folder, base, start_path, boardIds = bbsdef[bbsId]
curr_board = boardIds[boardId]

print('Updating [%s] <board: %d>.' % (bbsname, curr_board))

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
    favorites = load_data['favorites']
    blacklist = load_data['blacklist']
### 如果不存在json，初始化空数据
else:
    last_timestamp = 0
    last_threads = []
    tags = {}
    favorites = ['琼明神女录', '册母为后', '绿帽武林之淫乱后宫', '龙珠肏', '锦绣江山传', '淫堕的女武神']
    blacklist = []

# In[102]:


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
    print('] last update at [%s], this update will load <%d> panges' % (tmstr, pages))
else:
    print('] manual set, update <%d> pages.' % (pages))


# In[103]:


### 获取html的爬虫类
class Crawler:
    
    def __init__(self, delay):
        
        proxy_handler = request.ProxyHandler({
            'http':'http://127.0.0.1:8080',
            'https':'https://127.0.0.1.8080'
        })
        opener = request.build_opener(proxy_handler)
        request.install_opener(opener)

        self.delay = delay
        self.last_accessed = time.time()

    def getUrl(self, url):

        if self.delay > 0:
            sleep_secs = self.delay - (time.time() - self.last_accessed)

            if sleep_secs > 0:
                time.sleep(sleep_secs) 
        self.last_accessed = time.time()
        
        head = {
            'User-Agent': 'Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.94 Safari/537.36'
        }

        req = request.Request(url, headers=head)
        response = request.urlopen(req)

        html = response.read().decode('gbk')
        print('Get [%s] OK' % url)
        return html


# In[104]:


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


# In[105]:


crawler = Crawler(10)


# In[106]:


### 读取新数据
latest_threads = []
for i in range(0, pages):
    url = base + (start_path % (curr_board, (i+1)))
    latest_threads += bbsdoc(crawler.getUrl(url))


# In[107]:


### 合并新旧数据
def merge(lasts, latest):
    threads = lasts[:]
    
    lastIds = [x['threadId'] for x in lasts]
    for t in latest:
        if not t['threadId'] in lastIds:
            threads.append(t)
    return threads

threads = merge(last_threads, latest_threads)


# In[108]:


tags = {}
### 扫描新增数据，提取关键字
for i in range(len(threads)):
    t = threads[i]
    title = t['title']
    keywords = re.findall('【(.*?)】', title)
    for keyword in keywords:
        if not keyword.isdigit() and not keyword in blacklist:
            if not keyword in tags:
                tags[keyword] = []
            tags[keyword].append(i)

### 主题下，按照时间排序
for keyword in tags:
    tags[keyword].sort(key=lambda x: int(threads[x]['threadId']), reverse=True)

### 根据收藏夹，扫描所有文章，是否存在本地数据
for tag in favorites:
    if tag in tags:
        for i in tags[tag]:
            t = threads[i]
            txtpath = os.path.join(save_path, t['threadId'] + '.txt')
            if not os.path.exists(txtpath):
                url = base + t['link']
                chapter = bbscon(crawler.getUrl(url))
                with open(txtpath, 'w', encoding='utf-8') as f:
                    f.write(chapter)
        print('keyword [%s] saved.' % tag)



# In[109]:


### 保存data
with open(meta_data_path, 'w', encoding='utf-8') as f:
    save_data = {
        'timestamp': time.time(),
        'threads': threads,
        'tags': tags,
        'favorites': favorites,
        'blacklist': blacklist,
    }
    json.dump(save_data, f)


# In[ ]:





# In[ ]:




