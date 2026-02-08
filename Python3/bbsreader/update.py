#!/usr/bin/env python
# coding: utf-8
import sys
from pathlib import Path
from bs4 import BeautifulSoup
import argparse

from bbsreader_lib import *


### 参数
parser = argparse.ArgumentParser()
parser.add_argument(
    "pages", nargs="?", type=int, help="manual set download pages number"
)
parser.add_argument("start", nargs="?", type=int, help="manual set download start page")
parser.add_argument("-l", "--list", action="store_true", help="support BBS list")
parser.add_argument("--bbsid", type=int, default=0, help="manual set <BBS ID>")
parser.add_argument("--boardid", type=int, default=None, help="manual set <BOARD ID>")
args = parser.parse_args()

pages = args.pages
start = args.start
bbsId = args.bbsid
boardId = args.boardid

if args.list:
    print("%s\n" % "\n".join([str(x) for x in bbsdef]))
    sys.exit(0)

crawler = Crawler.getCrawler(bbsId)
if not crawler:
    print("get crawler failed.")
    sys.exit(1)

meta_data = MetaData(save_root_path)

### 根据上次更新时间，确定这次读取几页
if pages is None:
    now = time.time()
    if meta_data.last_timestamp == 0:
        pages = 100
    elif now - meta_data.last_timestamp < 60 * 60 * 24 * 10:  # 没超过10天，查看5页
        pages = 5
    else:
        pages = 30
    timestr = time.strftime(
        "%Y-%m-%d %H:%M:%S", time.localtime(meta_data.last_timestamp)
    )
    start = 0
    print("] last update at [%s], this update will load <%d> pages" % (timestr, pages))
else:
    if start is None:
        start = 0
    print("] manual set, update <%d> pages, from <%d>." % (pages, start))


### 读取版面
def bbsdoc(html, siteId):
    soup = BeautifulSoup(html, "html5lib")
    objs = soup.select("tbody[id^=stickthread_]")
    objs += soup.select("tbody[id^=normalthread_]")
    threads = []
    for t in objs:
        title = t.select("th span[id^=thread_] a")[0].text
        link = t.select("th span[id^=thread_] a")[0]["href"]
        threadId = link.split("-")[1]
        author = t.select("td.author cite a")[0].text.strip()
        postTime = t.select("td.author em")[0].text
        threads.append(MakeThread(siteId, threadId, title, author, postTime, link))
    return threads


### 读取文章
def bbscon(html):
    soup = BeautifulSoup(html, "html5lib")
    posts = soup.select("div[id^=postmessage_] div[id^=postmessage_]")
    if len(posts) > 0:
        postobj = posts[0]
        [x.decompose() for x in postobj.select("strong")]
        [x.decompose() for x in postobj.select("table")]
        return postobj.text
    else:
        boxmsg = soup.select("div.box.message")
        if len(boxmsg) > 0:
            return ""
        else:
            voteposts = soup.select("div.postmessage")
            if len(voteposts) > 0:
                votepostobj = voteposts[0]
                [x.decompose() for x in votepostobj.select("strong")]
                [x.decompose() for x in votepostobj.select("table")]
                [x.decompose() for x in votepostobj.select("form#poll")]
                [x.decompose() for x in votepostobj.select("fieldset")]
                return votepostobj.text
            else:
                raise IOError("load html error.")


### 读取新数据
def update_threads(boardId, threads):
    print("Updating [%s] <board: %d>." % (crawler.bbsname, boardId))
    for i in range(start, start + pages):
        url = crawler.index_page % (boardId, (i + 1))
        try:
            threads += bbsdoc(crawler.getUrl(url), crawler.siteId)
        except Exception as e:
            print("Get [%s]: %s" % (url, str(e)))


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
    crawler = Crawler.getCrawler(t["siteId"])
    if not crawler:
        return

    txt_path = Path(save_root_path) / t["siteId"] / (t["threadId"] + ".txt")
    if not txt_path.exists():
        txt_path.parent.mkdir(parents=True, exist_ok=True)
        # try:
        if 1:
            chapter = bbscon(crawler.getUrl(t["link"]))
            with open(txt_path, "w", encoding="utf-8") as f:
                f.write(chapter)
        # except Exception as e:
        #     print('Get [%s]: %s' % (t['link'], str(e)))


for superkeyword in meta_data.superkeywords:
    if superkeyword["skType"] != SK_Type.Manual:
        for i in superkeyword["tids"]:
            t = meta_data.last_threads[i]
            download_article(t)
        print("superkeyword [%s] saved." % keytext(superkeyword))

meta_data.last_timestamp = time.time()
meta_data.save_meta_data()
