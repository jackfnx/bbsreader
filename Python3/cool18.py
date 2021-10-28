import sys
import argparse
import requests
import urllib
from bs4 import BeautifulSoup

from bbsreader_lib import *


class Crawler:
    def __init__(self):
        self.head = {
            'User-Agent': 'Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.94 Safari/537.36'
        }

        self.proxy = {
            "http": "http://127.0.0.1:8080",
            "https": "https://127.0.0.1:8080"
        }
        self.session = requests.session()
        self.siteId = 'cool18'
    
    def get(self, url):

        try:
            response = self.session.get(url, headers=self.head, proxies=self.proxy)
            html = response.content.decode('utf-8', 'ignore')
            print('Get [%s]: OK.' % (url))
            return html
        except Exception as e:
            print('Get [%s]: %s' % (url, str(e)))

crawler = Crawler()

def parse_title_str(title_str):
    if '【' in title_str and '】' in title_str:
        s = title_str.index('【')
        e = title_str.index('】')
        title = title_str[s+1:e]
        if '作者：' in title_str:
            c = title_str.index('作者：')
            author = title_str[c+3:].strip()
        elif '作者:' in title_str:
            c = title_str.index('作者:')
            author = title_str[c+3:].strip()
        else:
            author = '*'
    else:
        title = title_str
        author = '*'
    return title, author

def save_article(t, text):
    save_path = os.path.join(save_root_path, t['siteId'])

    txtpath = os.path.join(save_root_path, t['siteId'], t['threadId'] + '.txt')
    if not os.path.exists(txtpath):
        if not os.path.isdir(save_path):
            os.makedirs(save_path)
        with open(txtpath, 'w', encoding='utf-8') as f:
            f.write(text)

def verify_url(url):
    o = urllib.parse.urlparse(url)
    if o.netloc != 'www.cool18.com':
        return False
    return True

def parse_url(url):
    o = urllib.parse.urlparse(url)
    if o.netloc != 'www.cool18.com':
        print('ERROR: site is not www.cool18.com, [%s]' % o.netloc)
        sys.exit(1)
    postUrl = o.path + '?' + o.query
    params = urllib.parse.parse_qs(o.query)
    threadId = params['tid'][0]
    return postUrl, threadId

def bbscon(input_url, new_threads, fwd_link=False):
    try:
        postUrl, threadId = parse_url(input_url)

        html = crawler.get(input_url)
        soup = BeautifulSoup(html, 'html5lib')

        title_str = soup.select('td.show_content > center')[0].text
        title, author = parse_title_str(title_str)
        postTime = '1970-1-2'

        pre = soup.select('td.show_content > pre')[0]
        [x.decompose() for x in pre.select('font[color="#E6E6DD"]')]
        [x.replace_with('\n') for x in pre.find_all(['br', 'p'])]
        text = pre.text

        if fwd_link:
            links = [x['href'] for x in pre.find_all('a')]
            
            ul0 = soup.select('ul')[0]
            for x in ul0.select('li > a'):
                y = urllib.parse.urljoin(input_url, x['href'])
                links.append(y)

            links = [x for x in links if verify_url(x)]
            links = list(reversed(links))
        else:
            links = []

        new_thread = MakeThread(crawler.siteId, threadId, title_str, author, postTime, postUrl)

        if not fwd_link and len(text) < 1000:
            return None, None

        new_threads.append(new_thread)
        save_article(new_thread, text)
        print('Article, Title: [%s], Text: <%d>.' % (title_str, len(text)))

        return title, links
    except  Exception as e:
        print('Get [%s]: %s' % (input_url, str(e)))
        return None, None

def bbstcon(input_url):
    new_threads = []
    title, links = bbscon(input_url, new_threads, fwd_link=True)
    for lnk in links:
        _, _ = bbscon(lnk, new_threads, fwd_link=False)
    new_threads = list(reversed(new_threads))
    return title, new_threads

def main(urls, title):

    new_threads = []
    for url in urls:
        t, ts = bbstcon(url)
        if title is None:
            title = t
        new_threads += ts

    meta_data = MetaData(save_root_path)
    meta_data.merge_threads(new_threads)

    last_tids = [(t['siteId'], t['threadId']) for t in meta_data.last_threads]
    new_tids = [last_tids.index((t['siteId'], t['threadId'])) for t in new_threads]

    exists_sks = [sk for sk in meta_data.superkeywords if sk['skType'] == SK_Type.Manual and sk['keyword'] == title]
    if len(exists_sks) != 0:
        superkeyword = exists_sks[0]
        superkeyword['tids'] = new_tids
    else:
        superkeyword = {
            'skType': SK_Type.Manual,
            'keyword': title,
            'author': ['*'],
            'tids': new_tids,
            'read': -1,
            'groups': [],
            'groupedTids': [],
        }
        meta_data.superkeywords.append(superkeyword)
    print('superkeyword [%s] saved.' % keytext(superkeyword))
    meta_data.save_meta_data()

if __name__=='__main__':
    ### 参数
    parser = argparse.ArgumentParser()
    parser.add_argument('urls', nargs='+', help='<urls>')
    parser.add_argument('-t', '--title', nargs='?', type=str, help='manual set title (when update index)')
    args = parser.parse_args()

    print(args.urls)
    
    # urls = [
    #     'https://www.cool18.com/bbs4/index.php?app=forum&act=threadview&tid=70315',
    #     'https://www.cool18.com/bbs4/index.php?app=forum&act=threadview&tid=13869433',
    #     'https://www.cool18.com/bbs4/index.php?app=forum&act=threadview&tid=14014890',
    # ]
    # title = '六朝系列'
    main(args.urls, args.title)
