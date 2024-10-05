import os
import sys
import argparse

from bbsreader_lib import *


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('bbsid', type=int, default=0, help='<BBS ID>')
    parser.add_argument('threadids', nargs='+', type=int, help='<Thread ID>')
    parser.add_argument('-P', '--print-only', action='store_true', help='print only')
    args = parser.parse_args()

    threadIds = [str(x) for x in args.threadids]
    bbsInfo = bbsdef[args.bbsid]
    print(bbsInfo)
    bbsId = bbsInfo[1]
    printOnly = args.print_only

    meta_data = MetaData(save_root_path)

    objects = {i:x for (i, x) in enumerate(meta_data.last_threads) if x['siteId'] == bbsId and x['threadId'] in threadIds}
    for i, obj in objects.items():
        print(obj)

    if printOnly:
        return

    # 临时存储tids
    restore_threads = {i: [meta_data.last_threads[y] for y in x['tids'] if y not in objects] for (i, x) in enumerate(meta_data.superkeywords)}

    meta_data.last_threads = [x for (i, x) in enumerate(meta_data.last_threads) if i not in objects]

    # 更新新的tids
    updated_thread_ids = [(x['siteId'], x['threadId']) for x in meta_data.last_threads]
    for i, x in restore_threads.items():
        tids = [updated_thread_ids.index((t['siteId'], t['threadId'])) for t in x]
        meta_data.superkeywords[i]['tids'] = tids

    meta_data.merge_threads([])

    for i, obj in objects.items():
        txtpath = os.path.join(save_root_path, bbsId, '%s.txt' % obj['threadId'])
        if os.path.exists(txtpath):
            os.remove(txtpath)
            print('%s deleted.' % txtpath)
        else:
            print('%s not exists.' % txtpath)
    meta_data.save_meta_data()


if __name__=='__main__':
    main()
