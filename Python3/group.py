# coding: utf-8
import os
import sys
import time
import json
from fuzzywuzzy import fuzz

t0 = time.time()

save_root_path = 'C:/Users/hpjing/Dropbox/BBSReader.Cache'

meta_data_path = os.path.join(save_root_path, 'meta.json')

### 如果存在json，load数据
if os.path.exists(meta_data_path):
    with open(meta_data_path, encoding='utf-8') as f:
        load_data = json.load(f)
    timestamp = load_data['timestamp']
    threads = load_data['threads']
    tags = load_data['tags']
    superkeywords = load_data['superkeywords']
    blacklist = load_data['blacklist']
### 如果不存在json
else:
    sys.stderr.write('NO meta data.\n')
    sys.exit(0)


def in_group(i, groups):
    g = [x for x in groups if len([y for y in x if y==i]) > 0]
    return len(g) > 0


def do_grouping(ids, groups):

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
                tr = fuzz.ratio(t1['title'], t2['title'])
                if tr < 90:
                    continue
                ## 比较内容相似度
                cr = fuzz.ratio(text1, text2)
                # print('%s <%s> vs %s <%s>: title[%d], content[%d]' % (t1['title'], t1['siteId'], t2['title'], t2['siteId'], tr, cr))
                if cr > 70:
                    # print('%s <%s> vs %s <%s>: title[%d], content[%d]' % (t1['title'], t1['siteId'], t2['title'], t2['siteId'], tr, cr))
                    curr_group.append(key2)
                    curr_group_site_ids.append(t2['siteId'])
            ### 如果有空文，比较标题（严格相等才合并）
            else:
                if t1['title'] == t2['title']:
                    curr_group.append(key2)
                    curr_group_site_ids.append(t2['siteId'])
        if len(curr_group) > 1:
            groups.append(curr_group)


def keytext(superkeyword):
    if superkeyword['simple']:
        return superkeyword['keyword']
    else:
        return superkeyword['author'][0] + ":" + superkeyword['keyword']

for superkeyword in superkeywords:
    tids = superkeyword['tids']
    t_00 = time.time()
    do_grouping(tids, superkeyword['groups'])
    t_01 = time.time()
    print('superkeyword [%s]: grouped. (%d match, %.2fs)' % (keytext(superkeyword), len(superkeyword['groups']), t_01-t_00))


### 保存data
with open(meta_data_path, 'w', encoding='utf-8') as f:
    save_data = {
        'timestamp': timestamp,
        'threads': threads,
        'tags': tags,
        'superkeywords': superkeywords,
        'blacklist': blacklist
    }
    json.dump(save_data, f)


t1 = time.time()
print('total time: %.2fs' % (t1 - t0))
