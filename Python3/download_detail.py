#!/usr/bin/env python
# coding: utf-8
import os
import sys
import json
import re
from bs4 import BeautifulSoup
import argparse

from bbsreader_lib import *


### 参数
parser = argparse.ArgumentParser()
parser.add_argument('threadid', type=int, help='<Thread ID>')
parser.add_argument('bbsid', type=int, default=0, help='<BBS ID>')
args = parser.parse_args()

threadId = args.threadid
bbsId = args.bbsid


crawler = Crawler.getCrawler(bbsId)


### 读取文章
def bbstcon(html):
    soup = BeautifulSoup(html, 'html5lib')
    posts = soup.select('div[id^=postmessage_] div[id^=postmessage_]')
    if len(posts) > 0:
        floors = []
        for i, postobj in enumerate(posts):
            [x.decompose() for x in postobj.select('strong')]
            [x.decompose() for x in postobj.select('table')]
            s = postobj.text
            if i == 0:
                floors.append(s)
            elif len(s) > 1000:
                floors.append(s)
        return '\n\n\n\n-------------------------------------------------\n\n\n\n'.join(floors)
    else:
        boxmsg = soup.select('div.box.message')
        if len(boxmsg) > 0:
            return ''
        else:
            raise IOError('load html error.')


html = crawler.getUrl('thread-%d-1-1.html' % threadId)
text = bbstcon(html)
save_path = os.path.join(save_root_path, crawler.siteId)
txtpath = os.path.join(save_root_path, crawler.siteId, '%d.txt' % threadId)
with open(txtpath, 'w', encoding='utf-8') as f:
    f.write(text)
print('[%s] saved, (%d bytes)' % (txtpath, len(text)))
