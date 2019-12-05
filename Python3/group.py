# coding: utf-8
import os
import sys
import time
import json
import argparse
import re
from zhon.hanzi import punctuation as cn_punctuation
from string import punctuation as en_punctuation
from fuzzywuzzy import fuzz

from bbsreader_lib import *


def keytext(superkeyword):
    if superkeyword['simple']:
        return superkeyword['keyword']
    else:
        return superkeyword['author'][0] + ":" + superkeyword['keyword']


def in_group(i, groups):
    g = [x for x in groups if len([y for y in x if y==i]) > 0]
    return len(g) > 0


def clean_punc(line):
    return re.sub("[%s]+" % (cn_punctuation + en_punctuation), "", line)

 

def do_grouping_func(threads, ids, groups, save_root_path):

    texts = {}
    for i in ids:
        t = threads[i]
        txtpath = os.path.join(save_root_path, t['siteId'], t['threadId'] + '.txt')
        if not os.path.exists(txtpath):
            text = ''
        else:
            with open(txtpath, 'rb') as f:
                text = f.read().decode('gbk', 'ignore')
        key = t['siteId'] + '/' + t['threadId']
        texts[key] = (t, text)

    new_group_count = 0
    keys = list(texts.keys())
    for i in range(len(keys)):
        key1 = keys[i]
        t1, text1 = texts[key1]
        if in_group(key1, groups):
            continue
        curr_group = [key1]
        curr_group_site_ids = [t1['siteId']]
        for j in range(i+1, len(ids)):
            key2 = keys[j]
            t2, text2 = texts[key2]
            if t2['siteId'] in curr_group_site_ids or in_group(key2, groups):
                continue

            ### 如果两篇都不为空，比较内容
            if text1 != '' and text2 != '':
                ### 如果长度差的太多，不用比较内容
                lenr = len(text1) * 1.0 / len(text2) if len(text1) >= len(text2) else len(text2) * 1.0 / len(text1)
                if lenr > 1.5:
                    continue
                ### 比较标题相似度
                pure_title1 = clean_punc(t1['title'])
                pure_title2 = clean_punc(t2['title'])
                tr = fuzz.ratio(pure_title1, pure_title2)
                if tr < 90:
                    continue
                ## 比较内容相似度
                cr = fuzz.ratio(text1, text2)
                # print('%s <%s> vs %s <%s>: title[%d], content[%d]' % (t1['title'], t1['siteId'], t2['title'], t2['siteId'], tr, cr))
                if cr > 70:
                    # print('%s <%s> vs %s <%s>: title[%d], content[%d]' % (t1['title'], t1['siteId'], t2['title'], t2['siteId'], tr, cr))
                    curr_group.append(key2)
                    curr_group_site_ids.append(t2['siteId'])
                    # print(t1['title'], t2['title'])
            ### 如果有空文，比较标题（严格相等才合并）
            else:
                if t1['title'] == t2['title']:
                    curr_group.append(key2)
                    curr_group_site_ids.append(t2['siteId'])
                    # print(t1['title'], t2['title'])
        if len(curr_group) > 1:
            groups.append(curr_group)
            new_group_count+=1
    return new_group_count


def print_groups(groups):
    print('[' + '\n'.join([str(x) for x in groups]) + ']')


def do_grouping(threads, superkeyword, save_root_path, silence=True):
    tids = superkeyword['tids']
    print('superkeyword [%s]: grouping...' % (keytext(superkeyword)))
    if not silence:
        print('The origin:')
        print_groups(superkeyword['groups'])
    t_00 = time.time()
    inc = do_grouping_func(threads, tids, superkeyword['groups'], save_root_path)
    t_01 = time.time()
    print('superkeyword [%s]: grouped. (%d match, %d new, %.2fs)' % (keytext(superkeyword), len(superkeyword['groups']), inc, t_01-t_00))
    if not silence:
        print('The result:')
        print_groups(superkeyword['groups'])


def main():
    t0 = time.time()

    meta_data = MetaData(save_root_path)

    ### 如果存在json，load数据
    if len(meta_data.last_threads) > 0:
        timestamp = meta_data.last_timestamp
        threads = meta_data.last_threads
        tags = meta_data.tags
        superkeywords = meta_data.superkeywords
        blacklist = meta_data.blacklist
    ### 如果不存在json
    else:
        sys.stderr.write('NO meta data.\n')
        sys.exit(0)


    parser = argparse.ArgumentParser()
    parser.add_argument('-l', '--list', action='store_true', help='superkeywords list')
    parser.add_argument('-k', '--kid', type=int, default=-1, help='manual set superkeyword')
    parser.add_argument('-p', '--printonly', action='store_true', help='only print, not save')
    args = parser.parse_args()

    if args.list:
        print('%s\n' % '\n'.join(['%-3d: %s' % (x, keytext(superkeywords[x])) for x in range(len(superkeywords))]))
        sys.exit(0)

    keywordId = args.kid
    printOnly = args.printonly

    if keywordId < 0:
        for superkeyword in superkeywords:
            do_grouping(threads, superkeyword, save_root_path, silence=True)
    elif keywordId > len(superkeywords):
        sys.stderr.write('ERROR: The keywordId is NOT FOUND.\n')
        sys.exit(0)
    else:
        superkeyword = superkeywords[keywordId]
        do_grouping(threads, superkeyword, save_root_path, silence=False)


    if not printOnly:
        meta_data.superkeywords = superkeywords
        meta_data.save_meta_data()


    t1 = time.time()
    print('total time: %.2fs' % (t1 - t0))


if __name__=='__main__':
    main()
    