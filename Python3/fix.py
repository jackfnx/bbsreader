# coding: utf-8
import os
import sys
import time
import json

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


# t1 = time.time()
# print('total time: %.2fs' % (t1 - t0))
