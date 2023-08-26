function extract() {
    switch (location.href) {
        case "https://www.pixiv.net/following/watchlist/novels":
            // 获取书籍列表
            console.log("document.readyState is " + document.readyState);
            if (document.readyState != "complete") {
                console.log("<quick return>");
                return;
            }
            let info = [];
            let book_titles = document.querySelectorAll('a[href^="/novel/series/"]:not([image])')
            for (let book_title of book_titles) {
                let title = book_title.textContent;
                let div = book_title.nextElementSibling;     // 同级下一个div元素
                let book_author = div.querySelector('a[href^="/users/"]:not(:has(div,img))'); // div内查找a元素
                let author = book_author.textContent;
                info.push({ title: title, author: author })
            }
            console.log(info)
            // 发送信息到background
            chrome.runtime.sendMessage({ novelListMeta: info });
            break;
        case "https://example.com/book/abc/chapters":
            // 获取书籍章节
            info.title = document.querySelector(".book-title").textContent;
            let chapters = document.querySelectorAll(".chapter");
            info.chapters = [];
            for (let chapter of chapters) {
                info.chapters.push(chapter.textContent);
            }
            break;
    }

}

window.addEventListener("load", extract);
extract();
