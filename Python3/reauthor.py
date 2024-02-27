#!/usr/bin/env python
# coding: utf-8
import sys
import datetime
from pathlib import Path
from bs4 import BeautifulSoup
import argparse

from bbsreader_lib import *


### 参数
parser = argparse.ArgumentParser()
parser.add_argument('start_date', type=datetime.date.fromisoformat, help='from start date')
args = parser.parse_args()

start_date = args.start_date

meta_data = MetaData(save_root_path)

crawlers = {}
def get_crawler(bbsId):
    if bbsId in crawlers:
        return crawlers[bbsId]
    else:
        return Crawler.getCrawler(bbsId)


### 读取作者
def read_author(html):
    soup = BeautifulSoup(html, 'html5lib')
    authors = soup.select('div.viewthread td.postauthor cite a')
    if len(authors) > 0:
        return authors[0].text
    else:
        return ""



for t in meta_data.last_threads:
    pd = datetime.datetime.strptime(t["postTime"], "%Y-%m-%d").date()
    if t["siteId"] not in ["sexinsex", "sis001"]:
        continue
    if pd < start_date:
        continue

    crawler = get_crawler(t["siteId"])
    new_author = read_author(crawler.getUrl(t['link']))
    if not new_author:
        continue
    if t["author"] != new_author:
        print("%s (%s => %s)" % (t["title"], t["author"], new_author))
        t["author"] = new_author


meta_data.last_timestamp = time.time()
meta_data.save_meta_data()
