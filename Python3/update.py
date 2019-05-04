#!/usr/bin/env python
# coding: utf-8
import os
import sys
from bs4 import BeautifulSoup
import argparse

from bbsreader_lib import *


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

meta_data = MetaData(save_root_path)

### 根据上次更新时间，确定这次读取几页
if pages is None:
    now = time.time()
    if meta_data.last_timestamp == 0:
        pages = 100
    elif now - meta_data.last_timestamp < 60*60*24*10: # 没超过10天，查看5页
        pages = 5
    else:
        pages = 30
    tmstr = time.strftime('%Y-%m-%d %H:%M:%S', time.localtime(meta_data.last_timestamp))
    print('] last update at [%s], this update will load <%d> pages' % (tmstr, pages))
else:
    print('] manual set, update <%d> pages.' % (pages))


crawler = Crawler.getCrawler(bbsId)


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
    posts = soup.select('div[id^=postmessage_] div[id^=postmessage_]')
    if len(posts) > 0:
        postobj = posts[0]
        [x.decompose() for x in postobj.select('strong')]
        [x.decompose() for x in postobj.select('table')]
        return postobj.text
    else:
        boxmsg = soup.select('div.box.message')
        if len(boxmsg) > 0:
            return ''
        else:
            raise IOError('load html error.')


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


meta_data.merge_threads(latest_threads)


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

def keytext(superkeyword):
    if superkeyword['simple']:
        return superkeyword['keyword']
    else:
        return superkeyword['author'][0] + ":" + superkeyword['keyword']

for superkeyword in meta_data.superkeywords:
    for i in superkeyword['tids']:
        t = meta_data.last_threads[i]
        download_article(t)
    print('superkeyword [%s] saved.' % keytext(superkeyword))

meta_data.save_meta_data()
