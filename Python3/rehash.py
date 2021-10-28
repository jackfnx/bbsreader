import sys
import urllib
from bs4 import BeautifulSoup

from bbsreader_lib import *


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

url = sys.argv[1]

bbsId, threadId, postUrl = parse_url(url)

crawler = Crawler.getCrawler(bbsId)

html = crawler.getUrl(postUrl)

title, author, postTime = bbscon(html)

new_threads = [MakeThread(crawler.siteId, threadId, title, author, postTime, postUrl)]
print(new_threads)

meta_data = MetaData(save_root_path)
meta_data.merge_threads(new_threads, force=True)
meta_data.save_meta_data()
