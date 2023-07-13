async function showChapters() {
    // 获取当前激活的选项卡
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });

    // 获取选项卡中当前页面的 HTML 内容
    const [{ result }] = await chrome.scripting.executeScript({
        target: { tabId: tab.id },
        func: (items) => document.body.innerHTML,
    });
    const html = result;

    const novelMeta = extractNovelMetaFromHTML(html);
    showNovelMetaFromHTML(novelMeta);
}

function extractNovelMetaFromHTML(html) {
    const parser = new DOMParser();
    const doc = parser.parseFromString(html, 'text/html');
    const bookTitle = doc.querySelector("h1.series-title").textContent.trim();
    const chapterLinks = doc.querySelectorAll('a.novel-title');
    const chapters = [];
    for (const link of chapterLinks) {
        const title = link.textContent.trim();
        const url = link.href;
        chapters.push({ title, url });
    }
    return { title: bookTitle, chapters: chapters };
}

function showSeriesList(seriesMetaList) {
    console.log(seriesMetaList)
    const seriesList = document.createElement('ul');
    const headerItem = document.createElement('li');
    headerItem.textContent = "...";
    headerItem.addEventListener("click", function () {
        // chrome.storage.local.get("info", function(result) {
        //   render(result.info);
        // });
        chrome.runtime.sendMessage({ action: "openMainPage" });
    });
    seriesList.appendChild(headerItem);
    for (const seriesMeta of seriesMetaList) {
        const seriesItem = document.createElement('li');
        const titleItem = document.createElement('div');
        const authorItem = document.createElement('div');
        titleItem.classList.add('novel-title');
        titleItem.textContent = seriesMeta.title;
        authorItem.classList.add('novel-author');
        authorItem.textContent = seriesMeta.author;
        seriesItem.appendChild(titleItem);
        seriesItem.appendChild(authorItem);
        seriesList.appendChild(seriesItem);
    }
    const popupBody = document.body;
    popupBody.innerHTML = '';
    popupBody.appendChild(seriesList);
}

// showChapters();

// 获取存储的数据
chrome.storage.local.get("series", function (result) {
    if (result.series) {
        showSeriesList(result.series);
    }
});
