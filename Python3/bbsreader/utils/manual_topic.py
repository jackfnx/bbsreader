import os
import json
from .misc import _load_a_file, SK_Type


def load_manual_topics(meta_data_path: str) -> dict[str, dict[str, str]]:
    manual_topics = {}
    manual_topics_dir = os.path.join(meta_data_path, "manual_topics")
    for manual_topic in os.listdir(manual_topics_dir):
        mt_json = os.path.join(manual_topics_dir, manual_topic)
        with open(mt_json, encoding="utf-8") as f:
            mt = json.load(f)
        manual_topics[mt["id"]] = mt
    return manual_topics


def save_manual_topcis(manual_topics, meta_data_path: str) -> None:
    manual_topics_dir = os.path.join(meta_data_path, "manual_topics")
    os.makedirs(manual_topics_dir, exist_ok=True)

    mts_rm = [
        os.path.join(manual_topics_dir, x)
        for x in os.listdir(manual_topics_dir)
        if x[: -len(".json")] not in manual_topics
    ]
    for t in mts_rm:
        os.unlink(t)

    for manual_topic in manual_topics.values():
        mt_json_s = json.dumps(manual_topic, indent=2)
        mt_json = os.path.join(manual_topics_dir, manual_topic["id"] + ".json")
        s = _load_a_file(mt_json)
        if s != mt_json_s:
            with open(mt_json, "w", encoding="utf-8") as f:
                f.write(mt_json_s)


def __manual_topic_id(sk: dict[str, str]) -> str:
    return (
        "%s_%s" % (sk["author"][0], sk["keyword"])
        if sk["author"][0] != "*"
        else sk["keyword"]
    )


def _load_mts(
    siteId: str,
    superkeywords: list[dict[str, str]],
    manual_topics: dict[str, dict[str, str]],
) -> None:
    mts = []
    for i, sk in enumerate(superkeywords):
        if sk["skType"] == SK_Type.Manual and sk["keyword"] != "*":
            mt = manual_topics[__manual_topic_id(sk)]
            if mt["siteId"] == siteId:
                mts.append((i, mt))
    return mts


def _find_mt(
    sk: dict[str, str], manual_topics: dict[str, dict[str, str]]
) -> tuple[str, str]:
    if sk["skType"] == SK_Type.Manual and sk["keyword"] != "*":
        mtId = __manual_topic_id(sk)
        if mtId in manual_topics:
            return mtId, manual_topics[mtId]
        else:
            return mtId, None
    else:
        return None, None
