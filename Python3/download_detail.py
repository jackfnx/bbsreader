#!/usr/bin/env python
# coding: utf-8
import os
import sys
from bs4 import BeautifulSoup
import argparse

from bbsreader_lib import *


### 参数
parser = argparse.ArgumentParser()
parser.add_argument('threadid', type=int, help='<Thread ID>')
parser.add_argument('bbsid', type=int, default=0, help='<BBS ID>')
parser.add_argument('-u', '--updateindex', action='store_true', help='update index')
parser.add_argument('-t', '--title', nargs='?', type=str, help='manual set title (when update index)')
parser.add_argument('-a', '--author', nargs='?', type=str, help='manual set author (when update index)')
parser.add_argument('-p', '--posttime', nargs='?', type=str, help='manual set post time (when update index)')
args = parser.parse_args()

threadId = str(args.threadid)
bbsId = args.bbsid
updateIndex = args.updateindex


crawler = Crawler.getCrawler(bbsId)


### 读取文章
def bbstcon(html, page):
    soup = BeautifulSoup(html, 'html5lib')

    titles = soup.select('div[id=nav]')
    if len(titles) > 0:
        titleobj = titles[0]
        [x.decompose() for x in titleobj.select('a')]
        title = titleobj.text
        title = title.replace('»', '')
        title = title.strip()
    else:
        title = None

    authors = soup.select('td.postauthor cite a')
    if len(authors) > 0:
        author = authors[0].text
    else:
        author = 'unknown'

    postinfos = soup.select('div.postinfo')
    if len(postinfos) > 0:
        postinfo = postinfos[0]
        [x.decompose() for x in postinfo.select('strong')]
        [x.decompose() for x in postinfo.select('em')]
        [x.decompose() for x in postinfo.select('a')]
        postTime = postinfo.text
        postTime = postTime.strip()
        prefix = '发表于 '
        if postTime.startswith(prefix):
            postTime = postTime[len(prefix):]
        if ' ' in postTime:
            postTime = postTime[:postTime.index(' ')]
    else:
        postTime = '1970-1-1'

    posts = soup.select('div[id^=postmessage_] div[id^=postmessage_]')
    if len(posts) > 0:
        floors = []
        for i, postobj in enumerate(posts):
            [x.decompose() for x in postobj.select('strong')]
            [x.decompose() for x in postobj.select('table')]
            s = postobj.text
            if (i == 0) and (page == 1):
                floors.append(s)
            elif len(s) > 1000:
                floors.append(s)
        text = '\n\n\n\n-------------------------------------------------\n\n\n\n'.join(floors)
    else:
        boxmsg = soup.select('div.box.message')
        if len(boxmsg) > 0:
            text = ''
        else:
            raise IOError('load html error.')
    return text, title, author, postTime

def getpage_num(html):
    soup = BeautifulSoup(html, 'html5lib')
    pages = soup.select('div[class=pages] a')
    if len(pages) > 1:
        return int(pages[-2].text)
    else:
        return 1

def getpage(crawler, threadId, page):
    postUrl = 'thread-%s-%d-1.html' % (threadId, page)
    html = crawler.getUrl(postUrl)
    page_num = getpage_num(html) if page == 1 else -1
    text, title, author, postTime = bbstcon(html, page)
    return text, title, author, postTime, postUrl, page_num


text, title, author, postTime, postUrl, page_num = getpage(crawler, threadId, 1)
for page in range(2, page_num+1):
    nextPage, _, _, postTime, _, _ = getpage(crawler, threadId, page)
    text += '\n\n\n\n-------------------------------------------------\n\n\n\n'
    text += nextPage
save_path = os.path.join(save_root_path, crawler.siteId)
txtpath = os.path.join(save_root_path, crawler.siteId, '%s.txt' % threadId)
with open(txtpath, 'w', encoding='utf-8') as f:
    f.write(text)
print('[%s] saved, (%d bytes), %s' % (txtpath, len(text), title))

if updateIndex:
    if args.title:
        title = args.title
    if args.author:
        author = args.author
    if args.posttime:
        postTime = args.posttime
    new_threads = [MakeThread(crawler.siteId, threadId, title, author, postTime, postUrl)]

    meta_data = MetaData(save_root_path)
    meta_data.merge_threads(new_threads, force=True)
    meta_data.save_meta_data()
