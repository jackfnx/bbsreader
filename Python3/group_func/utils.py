
def keytext(superkeyword):
    if superkeyword['simple']:
        return superkeyword['keyword']
    else:
        return superkeyword['author'][0] + ":" + superkeyword['keyword']

