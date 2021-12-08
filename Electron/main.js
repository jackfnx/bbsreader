var electron = require('electron'); // electron 对象的引用
const app = electron.app; // BrowserWindow 类的引用
const BrowserWindow = electron.BrowserWindow;

let mainWindow = null;

// 监听应用准备完成的事件
app.on('ready', function() {
    // 创建窗口
    mainWindow = new BrowserWindow({width: 800, height: 600});
    mainWindow.loadFile('index.html');
    mainWindow.on('closed', function () { mainWindow = null; })
})

// 监听所有窗口关闭的事件 
app.on('window-all-closed', function () {
    // On OS X it is common for applications and their menu bar
    // to stay active until the user quits explicitly with Cmd + Q 
    if (process.platform !== 'darwin') {
        app.quit(); 
    }
})
