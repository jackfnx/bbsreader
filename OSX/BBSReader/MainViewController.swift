//
//  ViewController.swift
//  BBSReader
//
//  Created by jinghp on 2018/9/18.
//  Copyright © 2018年 sixue. All rights reserved.
//

import Cocoa

struct MetaData : Codable {
    var timestamp: Float
    let threads: [BBSThread]
    let tags: [String: [Int]]
    let anthologies: [[Int]]
    let favorites: [String]
    let blacklist: [String]
    let followings: [SuperKeyword]
    let tag_groups: [String: [[Int]]]
    let anthology_groups: [String: [[Int]]]
}

struct BBSThread : Codable {
    var siteId: String
    var threadId: String
    var title: String
    var author: String
    var postTime: String
    var link: String
}

struct SuperKeyword : Codable {
    var keyword: String
    let author: [String]
}

struct ListItem {
    var Source: String
    var ThreadId: String
    var Title: String
    var Author: String
    var Time: String
    var Link: String
}

class MainViewController: NSViewController {

    override func viewDidLoad() {
        super.viewDidLoad()

        reload()
        
        // Do any additional setup after loading the view.
        tableView.delegate = self
        tableView.dataSource = self
    }

    override var representedObject: Any? {
        didSet {
        // Update the view, if already loaded.
        }
    }
    
    func reload() {
        
        let jsonPath = "/Users/jinghp/Dropbox/BBSReader.Cache/meta.json"
        let jsonData = NSData.init(contentsOfFile: jsonPath)
        let jsonDecoder = JSONDecoder()
        meta = try! jsonDecoder.decode(MetaData.self, from: jsonData! as Data)
        
        items.removeAll()
        for tag in meta?.tags ?? [:] {
            if (meta?.favorites.contains(tag.key) ?? false) {
                let tid = tag.value[0]
                let t = meta!.threads[tid]
                let item = ListItem(Source: t.siteId, ThreadId: t.threadId, Title: tag.key, Author: t.author, Time: t.postTime, Link: t.link)
                items.append(item)
            }
        }
        let anthologyIds = [Int](0...((meta?.followings.count ?? 1) - 1))
        for x in anthologyIds {
            let superKeyword = meta!.followings[x]
            let author = superKeyword.author[0]
            let keyword = superKeyword.keyword
            
            var title : String
            if (keyword == "*") {
                title = ("【" + author + "】的作品集")
            }
            else if (author == "*") {
                title = ("专题：【" + keyword + "】")
            }
            else {
                title = keyword
            }
            
            let tid = meta!.anthologies[x][0]
            let t = meta!.threads[tid]
            
            let item = ListItem(Source: t.siteId, ThreadId: t.threadId, Title: title, Author: author, Time: t.postTime, Link: t.link)
            items.append(item)
        }
        
        let dateFormatter = DateFormatter()
        dateFormatter.dateFormat = "yyyy-MM-dd"
        
        items.sort(by: {
            let t0 = dateFormatter.date(from: $0.Time) ?? Date()
            let t1 = dateFormatter.date(from: $1.Time) ?? Date()
            
            let tid0 = Int($0.ThreadId) ?? 0
            let tid1 = Int($1.ThreadId) ?? 0
            
            if (t0.compare(t1) != .orderedSame) {
                return t0.compare(t1) == .orderedDescending
            }
            else {
                return tid0 > tid1
            }
        })
        
        tableView.reloadData()
    }
    
    var meta : MetaData?
    var items = [ListItem]()

    @IBOutlet weak var tableView: NSTableView!
}

extension MainViewController: NSTableViewDataSource {
    func numberOfRows(in tableView: NSTableView) -> Int {
        return items.count
    }
}

extension MainViewController: NSTableViewDelegate {
    func tableView(_ tableView: NSTableView, viewFor tableColumn: NSTableColumn?, row: Int) -> NSView? {
        
        if row >= items.count {
            return nil
        }
        
        let curr = items[row]
        
        var cellIdentifier: String = ""
        var cellValue: String = ""
        if tableColumn == tableView.tableColumns[0] {
            cellIdentifier = "Title"
            cellValue = curr.Title
        } else if tableColumn == tableView.tableColumns[1] {
            cellIdentifier = "Author"
            cellValue = curr.Author
        } else if tableColumn == tableView.tableColumns[2] {
            cellIdentifier = "Time"
            cellValue = curr.Time
        } else if tableColumn == tableView.tableColumns[3] {
            cellIdentifier = "Link"
            cellValue = curr.Link
        } else if tableColumn == tableView.tableColumns[4] {
            cellIdentifier = "Source"
            cellValue = curr.Source
        } else {
            return nil
        }
    
        if let cell = tableView.makeView(withIdentifier: NSUserInterfaceItemIdentifier(rawValue: cellIdentifier), owner: nil) as? NSTableCellView {
            cell.textField?.stringValue = cellValue
            return cell
        } else {
            return nil
        }
    }
}
