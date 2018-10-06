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
    anthologies = load_data['anthologies']
    favorites = load_data['favorites']
    blacklist = load_data['blacklist']
    followings = load_data['followings']
### 如果不存在json
else:
    sys.stderr.write('NO meta data.\n')
    sys.exit(0)

def do_grouping(ids):
    groups = []

    def in_group(i):
        g = [x for x in groups if len([y for y in x if y==i]) > 0]
        return len(g) > 0

    texts = []
    for i in ids:
        t = threads[i]
        txtpath = os.path.join(save_root_path, t['siteId'], t['threadId'] + '.txt')
        if not os.path.exists(txtpath):
            text = ''
        else:
            with open(txtpath, 'rb') as f:
                text = f.read().decode('gbk', 'ignore')
        texts.append(text)

    for i in range(len(ids)):
        if in_group(ids[i]):
            continue
        t1 = threads[ids[i]]
        curr_group = [ids[i]]
        curr_group_site_ids = [t1['siteId']]
        for j in range(i+1, len(ids)):
            t2 = threads[ids[j]]
            if t2['siteId'] in curr_group_site_ids:
                continue

            text1 = texts[i]
            text2 = texts[j]
            ### 如果两篇都不为空，比较内容
            if text1 != '' and text2 != '':
                ### 如果长度差的太多，不用比较内容
                lenr = len(text1) * 1.0 / len(text2) if len(text1) >= len(text2) else len(text2) * 1.0 / len(text1)
                if lenr > 1.5:
                    continue
                ### 比较内容相似度
                r = fuzz.ratio(text1, text2)
                print('%s <%s> vs %s <%s>: %d' % (t1['title'], t1['siteId'], t2['title'], t2['siteId'], r))
                # print(curr_group_site_ids)
                # print(t1['siteId'], t2['siteId'])
                if r > 90:
                    curr_group.append(ids[j])
                    curr_group_site_ids.append(t2['siteId'])
                else:
                    print('[DIFF]: length: %d vs %d, ratio: %.04f.' % (len(text1), len(text2), lenr))
            ### 如果有空文，比较标题（严格相等才合并）
            else:
                if t1['title'] == t2['title']:
                    curr_group.append(ids[j])
                    curr_group_site_ids.append(t2['siteId'])
        if len(curr_group) > 1:
            groups.append(curr_group)
    return groups

groups = []
for tag in favorites:
    if tag in tags:
        t_00 = time.time()
        groups += do_grouping(tags[tag])
        t_01 = time.time()
        print('keyword [%s]: grouped. (%.2fs)' % (tag, t_01-t_00))

for key in anthologies:
    t_00 = time.time()
    groups += do_grouping(anthologies[key])
    t_01 = time.time()
    print('anthology [%s]: grouped. (%.2fs)' % (key, t_01-t_00))



### 保存data
with open(meta_data_path, 'w', encoding='utf-8') as f:
    save_data = {
        'timestamp': time.time(),
        'threads': threads,
        'tags': tags,
        'anthologies': anthologies,
        'favorites': favorites,
        'blacklist': blacklist,
        'followings': followings,
        'groups': groups,
    }
    json.dump(save_data, f)

t1 = time.time()
print('total time: %.2fs' % (t1 - t0))