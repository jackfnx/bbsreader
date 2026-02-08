import os
import json
from .misc import _load_a_file


def save_threads(last_threads: list[dict[str, str]], meta_data_path: str) -> None:

    threads_dir = os.path.join(meta_data_path, "threads")
    os.makedirs(threads_dir, exist_ok=True)

    threads = {}
    for i in range(0, len(last_threads), 1000):
        threads_fname = "%d-%s-x.json" % (i, last_threads[i]["threadId"])
        threads[threads_fname] = last_threads[i : i + 1000]

    threads_rm = [
        os.path.join(threads_dir, x)
        for x in os.listdir(threads_dir)
        if x not in threads
    ]
    for t in threads_rm:
        os.unlink(t)

    for threads_fname in threads:
        threads_json_s = json.dumps(threads[threads_fname], indent=2)
        threads_json = os.path.join(threads_dir, threads_fname)
        s = _load_a_file(threads_json)
        if s != threads_json_s:
            with open(threads_json, "w", encoding="utf-8") as f:
                f.write(threads_json_s)


def load_threads(meta_data_path: list) -> list[dict[str, str]]:
    last_threads = []
    threads_dir = os.path.join(meta_data_path, "threads")
    threads_fs = sorted(os.listdir(threads_dir), key=lambda x: int(x.split("-")[0]))
    for threads_fname in threads_fs:
        threads_json = os.path.join(threads_dir, threads_fname)
        with open(threads_json, encoding="utf-8") as f:
            last_threads += json.load(f)
    return last_threads
