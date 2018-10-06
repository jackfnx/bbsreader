import os
import yaml

class SexInSex_Login:
    def __init__(self, save_root_path):
        passwd_yaml = os.path.join(save_root_path, 'PASSWORD.yaml')
        with open(passwd_yaml) as f:
            passwd = yaml.load(f)
            self.name = passwd['SexInSex']['name']
            self.pw = passwd['SexInSex']['pw']

    def login(self, session, head, proxy):
        loginurl = 'http://www.sexinsex.net/bbs/logging.php?action=login&'
        loginparams = {
            'formhash': 'f33adb50',
            'referer': 'http://www.sexinsex.net/bbs/forum-372-1.html',
            'loginfield': 'username',
            'username': self.name,
            'password': self.pw,
            'questionid': 0,
            'anwser': '',
            'cookietime': 315360000,
            'loginmode': 'normal',
            'styleid': 0,
            'loginsubmit': True
        }

        resp = session.post(loginurl, data=loginparams, headers=head, proxies=proxy)
        print('[色中色] 登陆: %s' % (resp))