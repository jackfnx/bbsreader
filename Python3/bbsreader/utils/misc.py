import enum
import os
from pathlib import Path


class SK_Type(str, enum.Enum):
    Simple = "Simple"
    Advanced = "Advanced"
    Author = "Author"
    Manual = "Manual"


def _load_a_file(path: os.PathLike | Path) -> str:
    """尝试读取一个文件，如果不存在就返回空字符串

    Args:
        path (str): _description_

    Returns:
        str: _description_
    """
    path = Path(path)
    if path.exists():
        with open(path, encoding="utf-8") as f:
            s = f.read()
    else:
        s = ""
    return s
