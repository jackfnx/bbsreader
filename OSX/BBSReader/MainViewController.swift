//
//  ViewController.swift
//  BBSReader
//
//  Created by jinghp on 2018/9/18.
//  Copyright © 2018年 sixue. All rights reserved.
//

import Cocoa

//extension String.Encoding {
//    static let gb18030_2000 = String.Encoding(rawValue: CFStringConvertEncodingToNSStringEncoding(CFStringEncoding(CFStringEncodings.GB_18030_2000.rawValue)))
//}

struct ListItem {
    var Source: String
    var ThreadId: String
    var Title: String
    var Author: String
    var Time: String
    var Link: String
    var Tag: String
    var AnthologyId: Int
}

class MainViewController: NSViewController {

    override func viewDidLoad() {
        super.viewDidLoad()
        
        let mainStoryboard = NSStoryboard(name: "Main", bundle: nil)
        let listVC = mainStoryboard.instantiateController(withIdentifier: "ListViewController") as! ListViewController
        let docVC = mainStoryboard.instantiateController(withIdentifier: "DocViewController") as! DocViewController
        let conVC = mainStoryboard.instantiateController(withIdentifier: "ConViewController") as! ConViewController
        
        self.addChild(listVC)
        self.addChild(docVC)
        self.addChild(conVC)
        
        listVC.importData(loadMetaData())
        self.containerView.addSubview(children[0].view)
    }
    
    func loadMetaData() -> [ListItem] {
        var items = [ListItem]()
        for tag in Meta.shared.meta.tags {
            if (Meta.shared.meta.favorites.contains(tag.key) ) {
                let tid = tag.value[0]
                let t = Meta.shared.meta.threads[tid]
                let item = ListItem(Source: t.siteId, ThreadId: t.threadId, Title: tag.key, Author: t.author, Time: t.postTime, Link: t.link, Tag: tag.key, AnthologyId: -1)
                items.append(item)
            }
        }
        let anthologyIds = [Int](0...((Meta.shared.meta.followings.count) - 1))
        for x in anthologyIds {
            let superKeyword = Meta.shared.meta.followings[x]
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
            
            let tid = Meta.shared.meta.anthologies[x][0]
            let t = Meta.shared.meta.threads[tid]
            
            let item = ListItem(Source: t.siteId, ThreadId: t.threadId, Title: title, Author: author, Time: t.postTime, Link: t.link, Tag: "", AnthologyId: x)
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
        
        return items
    }
    
    func loadDocData(_ docItem: ListItem) -> [ListItem] {
        var tids = [Int]()
        if (docItem.AnthologyId < 0) {
            tids = Meta.shared.meta.tags[docItem.Tag] ?? tids
        } else {
            tids = Meta.shared.meta.anthologies[docItem.AnthologyId]
        }
        
        var items = [ListItem]()
        for tid in tids {
            let t = Meta.shared.meta.threads[tid]
            let item = ListItem(Source: t.siteId, ThreadId: t.threadId, Title: t.title, Author: t.author, Time: t.postTime, Link: t.link, Tag: "", AnthologyId: -1)
            items.append(item)
        }
        
        return items
    }
    
    func loadTextData(_ conItem: ListItem) -> String {
        let path = Meta.ROOT_PATH + "/" + conItem.Source + "/" + conItem.ThreadId + ".txt"
        
        if let textData = (NSData.init(contentsOfFile: path ) as Data?) {
            if let str = String(data:textData, encoding:String.Encoding.utf8) {
                return str
            }
        }
        return ""
    }
    
    enum AppStatus {
        case MAIN
        case CONTENT
        case READ
    }
    
    enum AppStatusChanges {
        case FORWARD
        case BACKWARD
        case NONE
    }
    
    var appStatus = AppStatus.MAIN
    
    override func keyUp(with event: NSEvent) {
        let LEFT = String(Character(UnicodeScalar(NSLeftArrowFunctionKey)!))
        let RIGHT = String(Character(UnicodeScalar(NSRightArrowFunctionKey)!))
        let RETURN = String(Character(UnicodeScalar(NSCarriageReturnCharacter)!))
        
        var changes: AppStatusChanges = .NONE
        
        if (event.characters == LEFT || event.characters == "e" || event.characters == "E" || event.characters == "q" || event.characters == "Q") {
            changes = .BACKWARD
        } else if (event.characters == RIGHT || event.characters == RETURN || event.characters == "r" || event.characters == "R") {
            changes = .FORWARD
        }
        
        switch appStatus {
        case .MAIN:
            if (changes == .FORWARD) {
                let listVC = self.children[0] as! ListViewController
                listVC.forward()
            }
        case .CONTENT:
            if (changes == .FORWARD) {
                let docVC = self.children[1] as! DocViewController
                docVC.forward()
            } else if (changes == .BACKWARD) {
                self.backFromDoc()
            }
        case .READ:
            if (changes == .BACKWARD) {
                self.backFromCon()
            }
        }
    }
    
    func gotoDoc(_ docItem: ListItem) {
        let listVC = self.children[0]
        let docVC = self.children[1] as! DocViewController
        docVC.importData(loadDocData(docItem))
        self.transition(from: listVC, to: docVC, options: .slideLeft, completionHandler: nil)
        appStatus = .CONTENT
    }
    
    func gotoCon(_ conItem: ListItem) {
        let docVC = self.children[1]
        let conVC = self.children[2] as! ConViewController
        conVC.importData(loadTextData(conItem))
        self.transition(from: docVC, to: conVC, options: .slideLeft, completionHandler: nil)
        appStatus = .READ
    }
    
    func backFromCon() {
        let docVC = self.children[1]
        let conVC = self.children[2]
        self.transition(from: conVC, to: docVC, options: .slideRight, completionHandler: nil)
        appStatus = .CONTENT
    }
    
    func backFromDoc() {
        let listVC = self.children[0]
        let docVC = self.children[1]
        self.transition(from: docVC, to: listVC, options: .slideRight, completionHandler: nil)
        appStatus = .MAIN
    }
    
    @IBOutlet var containerView: NSView!
}

