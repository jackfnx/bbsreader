from bbsreader_lib import *


meta_data = MetaData(save_root_path)
for sk in meta_data.superkeywords:
    if sk["simple"]:
        sk["skType"] = SK_Type.Simple
    else:
        sk["skType"] = SK_Type.Advanced
    del sk["simple"]
meta_data.save_meta_data()
