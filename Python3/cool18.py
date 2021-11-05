import sys
import argparse
from typing import NewType
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
        response = self.session.get(url, headers=self.head, proxies=self.proxy)
        html = response.content.decode('utf-8', 'ignore')
        return html

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
    elif '《' in title_str and '》' in title_str:
        s = title_str.index('《')
        e = title_str.index('》')
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
    if not os.path.isdir(save_path):
        os.makedirs(save_path)
    txtpath = os.path.join(save_root_path, t['siteId'], t['threadId'] + '.txt')
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
    params = urllib.parse.parse_qs(o.query)
    threadId = params['tid'][0]
    return o.query, threadId

def bbscon(url, fwd_link=False):
    postUrl, threadId = parse_url(url)

    html = crawler.get(url)
    soup = BeautifulSoup(html, 'html5lib')

    title_str = soup.select('td.show_content > center')[0].text
    title, author = parse_title_str(title_str)

    tab2 = soup.select('table')[1]
    poster = tab2.select('td > a')[0].text

    line = tab2.text.strip()
    postTime = line[line.index('] 于')+3: line.index('已读')].strip()
    postTime = time.strftime('%Y-%m-%d', time.strptime(postTime, '%Y-%m-%d %H:%M'))

    pre = soup.select('td.show_content > pre')[0]
    [x.decompose() for x in pre.select('font[color="#E6E6DD"]')]
    [x.decompose() for x in pre.select('font[color="E6E6DD"]')]
    [x.replace_with(x.text + '\n') for x in pre.find_all(['br', 'p'])]
    text = pre.text

    if fwd_link:
        links = [x['href'] for x in pre.find_all('a')]
        
        ul0 = soup.select('ul')[0]
        for x in ul0.select('li > a'):
            y = urllib.parse.urljoin(url, x['href'])
            links.append(y)

        links = [x for x in links if verify_url(x)]
        links = list(reversed(links))
    else:
        links = []

    new_thread = MakeThread(crawler.siteId, threadId, title_str, author, postTime, postUrl)
    return title_str, title, author, poster, links, text, new_thread

def bbstcon(url, trace, indent, bypass_urls):
    new_threads = []

    try:
        title_str, title, author, poster, links, text, new_thread = bbscon(url, fwd_link=(trace!=0))
        bypass_urls.append(url)

        print('%sGet: [%s], OK.' % (' '*indent, url))
        logger = '%sTitle: [%s], Text: [%d], Poster: [%s]' % (' '*indent, title_str, len(text), poster)
        if (len(text) > 1000):
            new_threads.append(new_thread)
            save_article(new_thread, text)
            save_flag = True
            print('%s, Saved.' % (logger))
        else:
            save_flag = False

    except Exception as e:
        print('Get [%s]: %s' % (url, str(e)))
        return None, None, new_threads

    for lnk in links:
        if url_in(lnk, bypass_urls):
            continue
        _, _, sub_threads = bbstcon(lnk, trace-1, indent+1, bypass_urls=bypass_urls)
        if not save_flag and len(sub_threads) > 0:
            new_threads.append(new_thread)
            save_article(new_thread, text)
            save_flag = True
            print('%s, Saved(for sub_threads).' % (logger))
        new_threads += sub_threads

    if not save_flag:
        print('%s, Not saved.' % (logger))

    return title, author, new_threads

def main(urls, title, author, trace):

    new_threads = []
    for url in urls.copy():
        t, a, ts = bbstcon(url, trace, 0, bypass_urls=urls)
        if title is None:
            title = t
        if author is None:
            author = a
        new_threads += ts
    new_threads = list(reversed(new_threads))

    meta_data = MetaData(save_root_path)
    meta_data.merge_threads(new_threads, force=True)

    last_tids = [(t['siteId'], t['threadId']) for t in meta_data.last_threads]
    new_tids = [last_tids.index((t['siteId'], t['threadId'])) for t in new_threads]

    exists_sks = [sk for sk in meta_data.superkeywords if sk['skType'] == SK_Type.Manual and sk['keyword'] == title]
    if len(exists_sks) != 0:
        superkeyword = exists_sks[0]
        superkeyword['author'] = [author]
        superkeyword['tids'] = new_tids
    else:
        superkeyword = {
            'skType': SK_Type.Manual,
            'keyword': title,
            'author': [author],
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
    parser.add_argument('--title', nargs='?', type=str, help='manual set title (when update index)')
    parser.add_argument('--author', nargs='?', type=str, help='manual set author (when update index)')
    parser.add_argument('--trace', default=1, type=int, help='manual set trace step')
    args = parser.parse_args()

    print(args.urls)

    # urls = [
    #     'https://www.cool18.com/bbs4/index.php?app=forum&act=threadview&tid=70315',
    #     'https://www.cool18.com/bbs4/index.php?app=forum&act=threadview&tid=13869433',
    #     'https://www.cool18.com/bbs4/index.php?app=forum&act=threadview&tid=14014890',
    # ]
    # title = '六朝系列'
    # author = '弄玉+龙璇'
    # trace = 1
    main(args.urls, args.title, args.author, args.trace)
