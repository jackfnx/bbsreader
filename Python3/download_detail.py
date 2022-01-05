#!/usr/bin/env python
# coding: utf-8
import os
import sys
import copy
from typing import NewType
from bs4 import BeautifulSoup
import argparse

from bbsreader_lib import *


### 参数
parser = argparse.ArgumentParser()
parser.add_argument('bbsid', type=int, default=0, help='<BBS ID>')
parser.add_argument('threadids', nargs='+', type=int, help='<Thread ID>')
parser.add_argument('-u', '--updateindex', action='store_true', help='update index')
parser.add_argument('-s', '--assingles', action='store_true', help='as singles topic')
parser.add_argument('-t', '--title', nargs='?', type=str, help='manual set title (when update index)')
parser.add_argument('-a', '--author', nargs='?', type=str, help='manual set author (when update index)')
parser.add_argument('-p', '--posttime', nargs='?', type=str, help='manual set post time (when update index)')
parser.add_argument('-m', '--maxpage', nargs='?', type=int, help='max page')
args = parser.parse_args()

threadIds = [str(x) for x in args.threadids]
bbsId = args.bbsid
updateIndex = args.updateindex
asSingles = args.assingles
maxPage = args.maxpage


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
        author = '*'

    floor_ids = []
    floors = []
    posts = soup.select('div[id^=postmessage_] div[id^=postmessage_]')
    if len(posts) > 0:
        for i, postobj in enumerate(posts):
            [x.decompose() for x in postobj.select('strong')]
            [x.decompose() for x in postobj.select('table')]
            s = postobj.text
            if (i == 0) and (page == 1):
                floors.append(s)
                floor_ids.append((i, len(s)))
            elif len(s) > 1000:
                floors.append(s)
                floor_ids.append((i, len(s)))
        text = '\n\n\n\n-------------------------------------------------\n\n\n\n'.join(floors)
    else:
        boxmsg = soup.select('div.box.message')
        if len(boxmsg) > 0:
            text = ''
        else:
            voteposts = soup.select('div.postmessage')
            if len(voteposts) > 0:
                votepostobj = voteposts[0]                
                [x.decompose() for x in votepostobj.select('strong')]
                [x.decompose() for x in votepostobj.select('table')]
                [x.decompose() for x in votepostobj.select('form#poll')]
                [x.decompose() for x in votepostobj.select('fieldset')]
                text = votepostobj.text
            else:
                raise IOError('load html error.')

    postinfos = soup.select('div.postinfo:not(.postactions)')
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
        postTime = '1970-1-2'

    return text, title, author, postTime, floor_ids

def getpage_num(html):
    soup = BeautifulSoup(html, 'html5lib')
    pages = soup.select('div[class=pages] a')
    if len(pages) > 1:
        try:
            t = pages[-1].text
            if t.startswith('... '):
                t = t[len('... '):]
            n = int(t)
        except:
            t = pages[-2].text
            if t.startswith('... '):
                t = t[len('... '):]
            n = int(t)
        return n
    else:
        return 1

def getpage(crawler, threadId, page):
    postUrl = 'thread-%s-%d-1.html' % (threadId, page)
    html = crawler.getUrl(postUrl)
    page_num = getpage_num(html) if page == 1 else -1
    text, title, author, postTime, floors = bbstcon(html, page)
    return text, title, author, postTime, postUrl, page_num, floors


new_threads = []

for threadId in threadIds:
    text, title, author, postTime, postUrl, page_num, floors = getpage(crawler, threadId, 1)
    print('thread: %s, page: %d/%d, new floors: %s.' % (threadId, 1, page_num, floors))
    page_num2 = page_num if maxPage is None else maxPage
    for page in range(2, page_num2+1):
        nextPage, _, _, _, _, _, floors = getpage(crawler, threadId, page)
        print('thread: %s, page: %d/%d, new floors: %s.' % (threadId, page, page_num, floors))
        text += '\n\n\n\n-------------------------------------------------\n\n\n\n'
        text += nextPage
    save_path = os.path.join(save_root_path, crawler.siteId)
    txtpath = os.path.join(save_root_path, crawler.siteId, '%s.txt' % threadId)
    with open(txtpath, 'w', encoding='utf-8') as f:
        f.write(text)
    print('[%s] saved, (%d bytes), %s' % (txtpath, len(text), title))

    if args.title:
        title = args.title
    if args.author:
        author = args.author
    if args.posttime:
        postTime = args.posttime
    new_threads.append(MakeThread(crawler.siteId, threadId, title, author, postTime, postUrl))
    print(new_threads[-1])

if updateIndex:
    meta_data = MetaData(save_root_path)
    meta_data.merge_threads(new_threads, force=True)
    if asSingles:
        sk = [x for x in meta_data.superkeywords if x['skType'] == SK_Type.Manual and x['keyword'] == 'Singles'][0]
        lastIds = [(x['siteId'], x['threadId']) for x in meta_data.last_threads]
        assert len(new_threads) == 1
        tid = lastIds.index((new_threads[0]['siteId'], new_threads[0]['threadId']))
        if tid not in sk['tids']:
            sk['tids'].append(tid)
            sk['groups'].append((tid,))
            ret = 'new'
        else:
            ret = 'existed'
        sk['tids'].sort(key=lambda x: meta_data.last_threads[x]['postTime'], reverse=True)
        sk_p = copy.deepcopy(sk)
        sk_p['tids'] = '<%d>' % len(sk_p['tids'])
        sk_p['groups'] = '<%d>' % len(sk_p['groups'])
        print(sk_p, ret)
    meta_data.save_meta_data()
