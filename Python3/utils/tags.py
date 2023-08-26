from collections import defaultdict
import os
import re
import json
import romkan
import pinyin
from tqdm import tqdm
from pathlib import Path


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


def _load_a_file(tag_path: str) -> str:
    """尝试读取一个文件，如果不存在就返回空字符串

    Args:
        tag_path (str): _description_

    Returns:
        str: _description_
    """
    if tag_path.exists():
        with open(tag_path, encoding='utf-8') as f:
            s = f.read()
    else:
        s = ''
    return s


def save_tags(tags: dict[str, list[int]], meta_data_path: str) -> None:

    tags_dir = os.path.join(meta_data_path, 'tags')
    os.makedirs(tags_dir, exist_ok=True)

    tags_queue: dict[Path, list[dict[str, str | list[int]]]] = defaultdict(list)
    for t, tv in tqdm(tags.items(), desc="prepare4save"):
        pre_s = _get_prefixes(_get_pinyin(t), n=3)
        tag_path = Path(os.path.join(tags_dir, pre_s[-1] + ".json"))
        tags_queue[tag_path].append({"tag": t, "tids": tv})

    for v in tags_queue.values():
        v.sort(key=lambda x: x["tag"])

    tags_rm = [x for x in Path(tags_dir).glob("**/*.json") if x not in tags_queue]
    for f in tqdm(tags_rm, desc="rm"):
        f.unlink()

    dirs_rm = [x for x in Path(tags_dir).glob("**") if x.is_dir and not list(x.iterdir())]
    for d in tqdm(dirs_rm, desc="rmdir"):
        d.rmdir()

    tags_path = sorted(list(tags_queue.keys()))
    count = 0
    counts = []
    for tag_path in tqdm(tags_path, desc="updating"):
        tags_json_s = json.dumps(tags_queue[tag_path], indent=2)
        tag_path.parent.mkdir(parents=True, exist_ok=True)
        s = _load_a_file(tag_path)
        if s != tags_json_s:
            count += 1
            with open(tag_path, 'w', encoding='utf-8') as f:
                f.write(tags_json_s)
        counts.append(len(s))
    print(f"updated {count}")


def load_tags(meta_data_path: str) -> dict[str, list[int]]:
    tags: dict[str, list[int]] = {}
    tags_dir = os.path.join(meta_data_path, 'tags')
    for tag_path in Path(tags_dir).glob("**/*.json"):
        with open(tag_path, encoding='utf-8') as f:
            tags_segs = json.load(f)
        for tag_seg in tags_segs:
            k, v = tag_seg["tag"], tag_seg["tids"]
            tags[k] = v

    return tags


if __name__=="__main__":
    from bbsreader_lib import MetaData, save_root_path

    meta_data = MetaData(save_root_path)
    meta_data_path = "/Users/apple/turboc"
    save_tags(meta_data.tags, meta_data_path)
    tags = load_tags(meta_data_path)
    print(f"loaded {len(tags)}")
