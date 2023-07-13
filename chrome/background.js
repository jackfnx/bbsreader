function find_in_list(title, novelList) {
  for (let novel of novelList) {
    if (title === novel.title) {
      return novel;
    }
  }
  return null;
}


function mergeMeta(oldNovelList, newNovelListMeta) {
  let newNovelList = oldNovelList.slice();
  for (let newNovelMeta of newNovelListMeta) {
    oldNovel = find_in_list(newNovelMeta.title, oldNovelList);
    if (oldNovel === null) {
      newNovelList.push(newNovelMeta);
    }
  }
  console.log(newNovelList);
  chrome.storage.local.set({ series: newNovelList });
}

// 接收消息并保存
chrome.runtime.onMessage.addListener(function (request, send, sendResponse) {
  console.log(request)
  if (request.novelListMeta) {
    chrome.storage.local.get("series", function (result) {
      console.log(result.series)
      mergeMeta(result.series, request.novelListMeta)
    });
  } else if (request.novelChapterList) {
    chrome.storage.local.get("series", function (result) {
      mergeChapters(result, request.novelChapterList)
    });
  } else if (request.action == "openMainPage") {
    chrome.tabs.create({ url: "main-page.html" });
  }
});
