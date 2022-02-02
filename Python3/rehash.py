import sys
import urllib
import argparse
from bs4 import BeautifulSoup

from bbsreader_lib import *


def main():
    ### 参数
    parser = argparse.ArgumentParser()
    parser.add_argument('bbsid', type=int, default=0, help='<BBS ID>')
    parser.add_argument('threadids', nargs='+', type=int, default=[], help='<Thread ID>')
    parser.add_argument('--url', nargs='?', type=str, help='<URL>')
    parser.add_argument('--sk', nargs='?', type=int, default=-1, help='<Superkeyword ID>')
    parser.add_argument('--printonly', '-P', action='store_true', help='print superkeyword id only.')
    parser.add_argument('--title', nargs='?', type=str, help='manual set title (when update index)')
    parser.add_argument('--author', nargs='?', type=str, help='manual set author (when update index)')
    parser.add_argument('--trace', default=1, type=int, help='manual set trace step')
    args = parser.parse_args()

    meta_data = MetaData(save_root_path)

    if args.printonly:
        if 0 <= args.sk < len(meta_data.superkeywords):
            sk = meta_data.superkeywords[args.sk]
            for tid in sk['tids']:
                t = meta_data.last_threads[tid]
                if '【' not in t['title'] and '】' not in t['title']:
                    print(t)
        else:
            for i, sk in enumerate(meta_data.superkeywords):
                print(i, sk['skType'], sk['author'][0], sk['keyword'])
        sys.exit(0)

    if 0 <= args.sk < len(meta_data.superkeywords):
        ts = []
        sk = meta_data.superkeywords[args.sk]
        for tid in sk['tids']:
            t = meta_data.last_threads[tid]
            if '【' not in t['title'] and '】' not in t['title']:
                bbsId = bbsdef_ids.index(t['siteId'])
                threadId = t['threadId']
                postUrl = 'thread-%s-%d-1.html' % (threadId, 1)
                ts.append((bbsId, threadId, postUrl))
    elif args.bbsid < 0:
        bbsId, threadId, postUrl = parse_url(args.url)
        ts = [(bbsId, threadId, postUrl)]
    else:
        ts = [(args.bbsid, str(t), 'thread-%s-%d-1.html' % (t, 1)) for t in args.threadids]

    new_threads = []
    for bbsId, threadId, postUrl in ts:

        crawler = Crawler.getCrawler(bbsId)

        html = crawler.getUrl(postUrl)

        title, author, postTime = bbscon(html)

        t = MakeThread(crawler.siteId, threadId, title, author, postTime, postUrl)
        print(t)
        new_threads.append(t)

    meta_data.merge_threads(new_threads, force=True)
    meta_data.save_meta_data()


def parse_url(url):
    o = urllib.parse.urlparse(url)
    postUrl = o.path.split('/')[-1]
    threadId = postUrl.split('-')[1]
    if o.netloc == 'www.sis001.com':
        return 0, threadId, postUrl
    elif o.netloc == 'www.sexinsex.net':
        return 1, threadId, postUrl
    else:
        print('ERROR: site is error, [%s]' % o.netloc)
        sys.exit(1)

### 读取文章
def bbscon(html):
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
        postTime = '1970-1-2'

    return title, author, postTime


if __name__=='__main__':
    main()
