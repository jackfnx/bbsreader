from collections import defaultdict
import os
import re
import json
import romkan
import pinyin
from tqdm import tqdm
from pathlib import Path
from .misc import _load_a_file


MODULE_TAG = "] tags/*.json I/O"

WIN32_CLIENT_LAUNCH = ("WIN32_CLIENT_LAUNCH" in os.environ and os.environ["WIN32_CLIENT_LAUNCH"])


def _get_pinyin(name: str) -> str:
    """转换成拼音（或日语罗马字），最终只会保留数字和大写字母

    Args:
        name (str): 文件名

    Returns:
        str: _description_
    """
    name = pinyin.get_initial(name)
    name = romkan.to_roma(name)
    name = re.sub(u"([^\u0030-\u0039\u0041-\u005a\u0061-\u007a])", "", name)
    name = name.upper()
    return name


def _get_prefixes(name: str, n: int = 1) -> tuple[str, ...]:
    """文件名前缀

    Args:
        name (str): _description_
        n (int, optional): _description_. Defaults to 1.

    Returns:
        tuple[str, ...]: 第1个字符，前2个字符，前3个字符，。。。前n个字符
    """
    pre = name[0].upper() if name else "_"
    if n == 1:
        return pre
    else:
        sub_pre = _get_prefixes(name[1:], n - 1)
        return tuple([pre] + [pre + x for x in sub_pre])


def save_tags(tags: dict[str, list[int]], meta_data_path: str) -> None:
    print(f"{MODULE_TAG}: save_tags()")

    tags_dir = os.path.join(meta_data_path, 'tags')
    os.makedirs(tags_dir, exist_ok=True)

    tags_queue: dict[Path, list[dict[str, str | list[int]]]] = defaultdict(list)
    for t, tv in tqdm(tags.items(), desc="prepare4save", disable=WIN32_CLIENT_LAUNCH):
        pre_s = _get_prefixes(_get_pinyin(t), n=3)
        tag_path = Path(os.path.join(tags_dir, pre_s[-1] + ".json"))
        tags_queue[tag_path].append({"tag": t, "tids": tv})

    for v in tags_queue.values():
        v.sort(key=lambda x: x["tag"])

    tags_rm = [x for x in Path(tags_dir).glob("**/*.json") if x not in tags_queue]
    if tags_rm:
        for f in tqdm(tags_rm, desc="rm", disable=WIN32_CLIENT_LAUNCH):
            f.unlink()

    dirs_rm = [x for x in Path(tags_dir).glob("**") if x.is_dir and not list(x.iterdir())]
    if dirs_rm:
        for d in tqdm(dirs_rm, desc="rmdir", disable=WIN32_CLIENT_LAUNCH):
            d.rmdir()

    tags_path = sorted(list(tags_queue.keys()))
    count = 0
    counts = []
    for tag_path in tqdm(tags_path, desc="updating", disable=WIN32_CLIENT_LAUNCH):
        tags_json_s = json.dumps(tags_queue[tag_path], indent=2)
        tag_path.parent.mkdir(parents=True, exist_ok=True)
        s = _load_a_file(tag_path)
        if s != tags_json_s:
            count += 1
            with open(tag_path, 'w', encoding='utf-8') as f:
                f.write(tags_json_s)
        counts.append(len(s))
    print(f"{MODULE_TAG}: updated {count}")


def load_tags(meta_data_path: str) -> dict[str, list[int]]:
    print(f"{MODULE_TAG}: load_tags()")
    tags: dict[str, list[int]] = {}
    tags_dir = os.path.join(meta_data_path, 'tags')
    tags_path = list(Path(tags_dir).glob("**/*.json"))
    for tag_path in tqdm(tags_path, disable=WIN32_CLIENT_LAUNCH):
        with open(tag_path, encoding='utf-8') as f:
            tags_segs = json.load(f)
        for tag_seg in tags_segs:
            k, v = tag_seg["tag"], tag_seg["tids"]
            tags[k] = v

    print(f"{MODULE_TAG}: loaded {len(tags)}")
    return tags


if __name__=="__main__":
    from bbsreader_lib import MetaData, save_root_path

    meta_data = MetaData(save_root_path)
    meta_data_path = "/Users/apple/turboc"
    save_tags(meta_data.tags, meta_data_path)
    tags = load_tags(meta_data_path)
