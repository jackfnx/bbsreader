function showNovelList(seriesList) {

    let novelItems = document.getElementById("novel-list");
    for (let series of seriesList) {

        const novelItem = document.createElement('div');
        novelItem.classList.add("novel-item");
        const novelHeader = document.createElement('div');
        novelHeader.classList.add("novel-header");
        const titleItem = document.createElement('div');
        const authorItem = document.createElement('div');
        titleItem.classList.add('novel-title');
        titleItem.textContent = series.title;
        authorItem.classList.add('novel-author');
        authorItem.textContent = series.author;

        novelHeader.appendChild(titleItem);
        novelHeader.appendChild(authorItem);
        novelHeader.addEventListener("click", function toggleChapterList() {
            novelItem.classList.toggle("expanded");
        });

        const novelChapters = document.createElement('ul');
        novelChapters.classList.add("chapter-list");
        clist = [{ title: "hehe" }, { title: "haha" }, { title: "hoho" }];
        for (chapter of clist) {
            const chpaterTitleItem = document.createElement('li');
            chpaterTitleItem.textContent = chapter.title;
            novelChapters.appendChild(chpaterTitleItem);
        }
        novelItem.appendChild(novelHeader);
        novelItem.appendChild(novelChapters);
        novelItems.appendChild(novelItem);
    }
}


document.addEventListener("DOMContentLoaded", function () {
    // 获取存储的数据
    chrome.storage.local.get("series", function (result) {
        if (result.series) {
            showNovelList(result.series);
        }
    });
});
