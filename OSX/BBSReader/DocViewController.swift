//
//  ContentViewController.swift
//  BBSReader
//
//  Created by jinghp on 2018/10/9.
//  Copyright © 2018年 sixue. All rights reserved.
//

import Cocoa

class DocViewController: NSViewController {
    override func viewDidLoad() {
        super.viewDidLoad()
        
        self.tableView.delegate = self
        self.tableView.dataSource = self
        
        self.tableView.doubleAction = #selector(performDoubleClick)
        
        self.nextResponder = self.parent
    }
    
    override func viewWillAppear() {
        super.viewWillAppear()
        if (self.updateFlag) {
            self.tableView.reloadData()
            self.updateFlag = false
        }
    }
    
    override func viewDidAppear() {
        super.viewDidAppear()
        self.view.becomeFirstResponder()
    }
    
    @objc func performDoubleClick() {
        forward()
    }
    
    func forward() {
        let row = self.tableView.selectedRow
        if (row >= 0 && row < self.items.count) {
            let conItem = self.items[row]
            
            let mainVC = self.parent as! MainViewController
            mainVC.gotoCon(conItem)
        }
    }
    
    private lazy var items:[ListItem] = []
    private var updateFlag = false
    
    func importData(_ items: [ListItem]) {
        self.items = items
        self.updateFlag = true
    }
    
    @IBOutlet weak var tableView: NSTableView!
}

extension DocViewController: NSTableViewDataSource {
    func numberOfRows(in tableView: NSTableView) -> Int {
        return self.items.count
    }
}

extension DocViewController: NSTableViewDelegate {
    func tableView(_ tableView: NSTableView, viewFor tableColumn: NSTableColumn?, row: Int) -> NSView? {
        
        if row >= self.items.count {
            return nil
        }
        
        let curr = self.items[row]
        
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

