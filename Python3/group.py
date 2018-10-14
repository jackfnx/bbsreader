# coding: utf-8
import os
import sys
import time
import json
import argparse
from fuzzywuzzy import fuzz
from group_func.grouping import do_grouping

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


parser = argparse.ArgumentParser()
parser.add_argument('-l', '--list', action='store_true', help='support BBS list')
parser.add_argument('-k', '--keywordid', type=int, default=-1, help='manual set <BBS ID>')
args = parser.parse_args()

if args.list:
    print('%s\n' % '\n'.join(['%-3d: %s' % (x, keytext(superkeywords[x])) for x in range(len(superkeywords))]))
    sys.exit(0)

keywordId = args.keywordid

if keywordId < 0:
    for superkeyword in superkeywords:
        do_grouping(threads, superkeyword, save_root_path, silence=True)
elif keywordId > len(superkeywords):
    sys.stderr.write('ERROR: The keywordId is NOT FOUND.\n')
    sys.exit(0)
else:
    superkeyword = superkeywords[keywordId]
    do_grouping(threads, superkeyword, save_root_path, silence=False)


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
