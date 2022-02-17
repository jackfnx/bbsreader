import sys
import argparse
import requests
import urllib
from bs4 import BeautifulSoup

from bbsreader_lib import *


class UrlCache:
    def __init__(self, threads=[], cached_urls=[], force=False):
        self.threads = threads
        self.tkeys = [(t['siteId'], t['threadId']) for t in threads]
        self.cached_urls = [] if force else cached_urls
        self.new_urls = []

    def url_in_cache(self, url, trace):
        if trace != 0:
            return False, None
        
        if not url_in(url, self.cached_urls):
            return False, None

        _, threadId = parse_url(url)
        if ('cool18', threadId) not in self.tkeys:
            return True, None
        else:
            tid = self.tkeys.index(('cool18', threadId))
            return True, self.threads[tid]
    
    def append_url(self, new_url):
        if url_in(new_url, self.cached_urls):
            return
        if url_in(new_url, self.new_urls):
            return
        self.new_urls.append(new_url)
    
    def export(self):
        return self.cached_urls + self.new_urls

def urls_to_entries(urls, default_closed=False):
    return [{'url': u, 'closed': default_closed} for u in urls]

def entries_to_urls(entries, only_open=False):
    return [e['url'] for e in entries if not (only_open and e['closed'])]

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
        title = title_str[s+1:e].strip()

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
        title = title_str[s+1:e].strip()

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
        if '作者：' in title_str:
            c = title_str.index('作者：')
            author = title_str[c+3:].strip()
        elif '作者:' in title_str:
            c = title_str.index('作者:')
            author = title_str[c+3:].strip()
        else:
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

def bbstcon(url, trace, indent, bypass_urls, url_cache):
    new_threads = []

    incache, cached_thread = url_cache.url_in_cache(url, trace)
    if incache:
        if cached_thread is not None:
            title, author = cached_thread['title'], cached_thread['author']
            new_threads.append(cached_thread)
            print('Get: [%s], Title: [%s], Cached.' % (url, title))
        else:
            title, author = None, None
            print('Get: [%s], Marked as empty.' % (url))

    else:
        try:
            title_str, title, author, poster, links, text, new_thread = bbscon(url, fwd_link=(trace!=0))
            bypass_urls.append(url)
            url_cache.append_url(url)

            print('%sGet: [%s], OK.' % (' '*indent, url))
            logger = '%sTitle: [%s], Text: [%d], Poster: [%s]' % (' '*indent, title_str, len(text), poster)
            if (len(text) > 1000) or indent == 0:
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
            _, _, sub_threads = bbstcon(lnk, trace-1, indent+1, bypass_urls=bypass_urls, url_cache=url_cache)
            if not save_flag and len(sub_threads) > 0:
                new_threads.append(new_thread)
                save_article(new_thread, text)
                save_flag = True
                print('%s, Saved(for sub_threads).' % (logger))
            new_threads += sub_threads

        if not save_flag:
            print('%s, Not saved.' % (logger))

    return title, author, new_threads

def new_topic(urls, title, author, trace):

    new_threads = []
    url_cache = UrlCache()
    entry_urls = urls.copy()
    for url in entry_urls:
        t, a, ts = bbstcon(url, trace, 0, bypass_urls=urls, url_cache=url_cache)
        if title is None:
            title = t
        if author is None:
            author = a
        new_threads += ts
    new_threads = list(reversed(new_threads))

    meta_data = MetaData(save_root_path)
    meta_data.merge_threads(new_threads, force=True)

    last_keys = [(t['siteId'], t['threadId']) for t in meta_data.last_threads]
    new_tids = [last_keys.index((t['siteId'], t['threadId'])) for t in new_threads]

    exists_sks = [sk for sk in meta_data.superkeywords if sk['skType'] == SK_Type.Manual and sk['keyword'] == title]
    if len(exists_sks) != 0:
        superkeyword = exists_sks[0]
        superkeyword['author'] = [author]
        superkeyword['tids'] = new_tids
        superkeyword['kws'] = [() for _ in new_tids]
    else:
        superkeyword = {
            'skType': SK_Type.Manual,
            'keyword': title,
            'author': [author],
            'alias': [],
            'subKeywords': None,
            'tids': new_tids,
            'kws': [() for _ in new_tids],
            'read': -1,
            'groups': [],
            'groupedTids': [],
        }
        meta_data.superkeywords.append(superkeyword)
    print('superkeyword [%s] saved.' % keytext(superkeyword))

    mtId, mt = meta_data.find_mt(superkeyword)
    if mt is None:
        mt = {'id': mtId, 'siteId': 'cool18', 'entries': urls_to_entries(entry_urls), 'trace': trace, 'cache': url_cache.export()}
        meta_data.manual_topics[mtId] = mt
    else:
        mt['entries'] = urls_to_entries(entry_urls)
        mt['cache'] = url_cache.export()
    print('manual_topic [%s] saved.' % mtId)

    meta_data.save_meta_data()

def show_list():
    meta_data = MetaData(save_root_path)
    mts = meta_data.load_mts('cool18')
    for i, mt in mts:
        print(i, mt['id'])

def refresh_topic(superkeywordId, force):
    meta_data = MetaData(save_root_path)
    superkeyword = meta_data.superkeywords[superkeywordId]
    mtId, mt = meta_data.find_mt(superkeyword)

    new_threads = []
    entry_urls = entries_to_urls(mt['entries'])
    url_cache = UrlCache(meta_data.last_threads, mt['cache'], force)
    urls = entry_urls.copy()
    for url in entry_urls:
        _, _, ts = bbstcon(url, mt['trace'], 0, bypass_urls=urls, url_cache=url_cache)
        new_threads += ts
    new_threads = list(reversed(new_threads))

    meta_data.merge_threads(new_threads, force=True)

    last_keys = [(t['siteId'], t['threadId']) for t in meta_data.last_threads]
    new_tids = [last_keys.index((t['siteId'], t['threadId'])) for t in new_threads]

    superkeyword['tids'] = new_tids
    superkeyword['kws'] = [() for _ in new_tids]
    print('superkeyword [%s] saved.' % keytext(superkeyword))

    mt['cache'] = url_cache.export()
    print('manual_topic [%s] saved.' % mtId)

    meta_data.save_meta_data()

def update_topic(superKeywordId, title, author):
    meta_data = MetaData(save_root_path)
    superkeyword = meta_data.superkeywords[superKeywordId]
    mtId, mt = meta_data.find_mt(superkeyword)


if __name__=='__main__':
    ### 参数
    parser = argparse.ArgumentParser()
    subparser = parser.add_subparsers(dest='subcmd', title='sub commands')
    parser_new = subparser.add_parser('new', help='create a new topic (by urls)')
    parser_new.add_argument('urls', nargs='+', help='<Urls>')
    parser_new.add_argument('--title', nargs='?', type=str, help='manual set title')
    parser_new.add_argument('--author', nargs='?', type=str, help='manual set author')
    parser_new.add_argument('--trace', default=1, type=int, help='manual set trace step')
    parser_list = subparser.add_parser('list', help='list all articles')
    parser_refresh = subparser.add_parser('refresh', help='refresh topic')
    parser_refresh.add_argument('superKeywordId', type=int, help='<SuperKeywordId>')
    parser_refresh.add_argument('--force', '-f', action='store_true', help='force refresh')
    parser_update = subparser.add_parser('update', help='update topic info')
    parser_update.add_argument('superKeywordId', type=int, help='<SuperKeywordId>')
    parser_update.add_argument('--title', nargs='?', type=str, help='manual set title')
    parser_update.add_argument('--author', nargs='?', type=str, help='manual set author')
    args = parser.parse_args()

    if args.subcmd == 'new':
        # urls = [
        #     'https://www.cool18.com/bbs4/index.php?app=forum&act=threadview&tid=70315',
        #     'https://www.cool18.com/bbs4/index.php?app=forum&act=threadview&tid=13869433',
        #     'https://www.cool18.com/bbs4/index.php?app=forum&act=threadview&tid=14014890',
        # ]
        # title = '六朝系列'
        # author = '弄玉+龙璇'
        # trace = 1
        new_topic(args.urls, args.title, args.author, args.trace)
    elif args.subcmd == 'list':
        show_list()
    elif args.subcmd == 'refresh':
        refresh_topic(args.superKeywordId, args.force)
    elif args.subcmd == 'update':
        update_topic(args.superKeywordId, args.title, args.author)
