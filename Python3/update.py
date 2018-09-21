#!/usr/bin/env python
# coding: utf-8

# In[100]:


import os
import sys
import time
import json
import re
from urllib import request
from bs4 import BeautifulSoup


# In[101]:


save_path = 'E:/turboc/sis001'

meta_data_path = os.path.join(save_path, 'meta.json')

### BBS列表
bbsdef = [
    ['第一会所', 'http://www.sis001.com/forum/', 'forum-%d-%d.html', [383,]],
    ['色中色', 'http://www.sexinsex.net/forum/', 'forum-%d-%d.html', [383,]],
]

### 参数
if len(sys.argv) < 3:
    sys.stderr.write('%r\n' % bbsdef)
    sys.exit(0)
    
# bbsId = int(sys.argv[1])
# boardId = int(sys.argv[2])
bbsId = 0
boardId = 0

_, base, start_page, boardIds = bbsdef[bbsId]
curr_board = boardIds[boardId]

### 如果存在json，load数据
if os.path.exists(meta_data_path):
    with open(meta_data_path) as f:
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
    favorites = ['琼明神女录', '册母为后', '绿帽武林之淫乱后宫', '龙珠肏', '锦绣江山传']
    blacklist = []


# In[102]:


### 根据上次更新时间，确定这次读取几页
now = time.time()
if last_timestamp == 0:
    pages = 100
elif now - last_timestamp < 60*60*24*10: # 没超过10天，查看5页
    pages = 5
else:
    pages = 30
    


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
    soup = BeautifulSoup(html)
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
    soup = BeautifulSoup(html)
    postobj = soup.select('div[id^=postmessage_] div[id^=postmessage_]')[0]
    [x.decompose() for x in postobj.select('strong')]
    [x.decompose() for x in postobj.select('table')]
    return postobj.text


# In[105]:


crawler = Crawler(10)


# In[106]:


### 读取新数据
latest_threads = []
for i in range(1, pages):
    url = base + (start_path % (curr_board, i))
    latest_threads += bbsdoc(crawler.getUrl(url))


# In[107]:


### 合并新旧数据
def merge(lasts, latest):
    threads = lasts[:]
    append_threads = []
    
    lastIds = [x['threadId'] for x in lasts]
    for t in latest:
        if not t['threadId'] in lastIds:
            threads.append(t)
            append_threads.append(t)
    return threads, append_threads

threads, append_threads = merge(last_threads, latest_threads)


# In[108]:


### 扫描新增数据，提取关键字
for t in append_threads:
    threadId = t['threadId']
    title = t['title']
    keywords = re.findall('【(.*?)】', title)
    for keyword in keywords:
        if not keyword.isdigit():
            if not keyword in tags:
                tags[keyword] = []
            tags[keyword].append(t)

### 根据收藏夹，扫描所有文章，是否存在本地数据
for fav in favorites:
    if fav in tags:
        for t in tags[fav]:
            txtpath = os.path.join(save_path, t['threadId'] + '.txt')
            if not os.path.exists(txtpath):
                url = base + t['link']
                chapter = bbscon(crawler.getUrl(url))
                with open(txtpath, 'w', encoding='utf-8') as f:
                    f.write(chapter)
        print('keyword [%s] saved.' % fav)


# In[109]:


### 保存data
with open(meta_data_path, 'w') as f:
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




