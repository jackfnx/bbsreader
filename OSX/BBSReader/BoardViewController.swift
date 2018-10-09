//
//  BoardViewController.swift
//  BBSReader
//
//  Created by jinghp on 2018/10/9.
//  Copyright © 2018年 sixue. All rights reserved.
//

import Cocoa

class BoardViewController: NSViewController {

    override func viewDidLoad() {
        super.viewDidLoad()

        // Do any additional setup after loading the view.
        tableView.delegate = self
        tableView.dataSource = self
        tableView.reloadData()
        
        tableView.doubleAction = #selector(performDoubleClick)
        
        self.nextResponder = self.parent
    }
    
    override func viewDidAppear() {
        super.viewDidAppear()
        self.view.becomeFirstResponder()
    }
    
    @objc func performDoubleClick() {
        forward()
    }
    
    func forward() {
        let row = tableView.selectedRow
        let item = Items[row]
        var contents = [Int]()
        if (item.AnthologyId < 0) {
            contents = Meta.shared.meta.tags[item.Tag] ?? contents
        } else {
            contents = Meta.shared.meta.anthologies[item.AnthologyId] 
        }
        
        var subList = [ListItem]()
        for tid in contents {
            let t = Meta.shared.meta.threads[tid]
            let item = ListItem(Source: t.siteId, ThreadId: t.threadId, Title: t.title, Author: t.author, Time: t.postTime, Link: t.link, Tag: "", AnthologyId: -1)
            subList.append(item)
        }
        
        let mainVC = self.parent as! MainViewController
        mainVC.showContent(subList)
    }
    
    lazy var Items:[ListItem] = []
    
    @IBOutlet weak var tableView: NSTableView!
}

extension BoardViewController: NSTableViewDataSource {
    func numberOfRows(in tableView: NSTableView) -> Int {
        return Items.count 
    }
}

extension BoardViewController: NSTableViewDelegate {
    func tableView(_ tableView: NSTableView, viewFor tableColumn: NSTableColumn?, row: Int) -> NSView? {
        
        if row >= Items.count {
            return nil
        }
        
        let curr = Items[row]
        
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
