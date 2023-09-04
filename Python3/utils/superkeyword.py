import os
import json
from dataclasses import dataclass, field
from dataclasses_json import DataClassJsonMixin
from .misc import SK_Type


@dataclass
class Superkeyword(DataClassJsonMixin):
    skType: SK_Type
    keyword: str
    author: list[str]
    alias: list[str]
    tids: list[int] = field(repr=False)
    groups: list[list[str]] = field(repr=False)
    kws: list[tuple[int, ...]] = field(repr=False)
    read: int = field(repr=False)
    subKeywords: list[list[str]] | None
    subReads: list[int] | None = field(repr=False)

    @property
    def author0(self) -> str:
        assert self.author
        return self.author[0]

    @property
    def keytext(self) -> str:
        if self.skType == SK_Type.Simple:
            return self.keyword
        else:
            return self.author0 + ":" + self.keyword

    @staticmethod
    def _find_sub_keywords(title, sub_keywords):
        res = []
        for j, s_kws in enumerate(sub_keywords):
            for s_kw in s_kws:
                if s_kw in title:
                    res.append(j)
                    break
        return tuple(res)


    def update(self, t: dict[str, str], tid: int, keywords: list[str]) -> None:
        title = t["title"]
        author = t["author"]

        if self.skType == SK_Type.Simple:
            if self.keyword in keywords:
                self.tids.append(tid)
                self.kws.append(())
            for a_alias in self.alias:
                if a_alias in keywords:
                    self.tids.append(tid)
                    self.kws.append(())
        elif self.skType == SK_Type.Advanced:
            if self.keyword in title:
                if self.author0 == "*" or self.author.count(author) > 0:
                    self.tids.append(tid)
                    self.kws.append(())
        elif self.skType == SK_Type.Author:
            if self.author.count(author) > 0:
                self.tids.append(tid)
                kws = Superkeyword._find_sub_keywords(title, self.sub_keywords)
                self.kws.append(kws)
        elif self.skType == SK_Type.Manual:
            pass
        else:
            pass


def save_superkeywords(superkeywords: list[Superkeyword], meta_data_path: str) -> None:
    superkeywords_json = os.path.join(meta_data_path, "superkeywords.json")
    s = Superkeyword.schema().dumps(superkeywords, many=True)
    with open(superkeywords_json, "w", encoding="utf-8") as f:
        f.write(s)


def load_superkeywords(meta_data_path: str) -> list[Superkeyword]:
    superkeywords_json = os.path.join(meta_data_path, "superkeywords.json")
    with open(superkeywords_json, encoding="utf-8") as f:
        s = f.read()
    superkeywords = Superkeyword.schema().loads(s, many=True)
    return superkeywords
