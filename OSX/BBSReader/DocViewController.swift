//
//  ContentViewController.swift
//  BBSReader
//
//  Created by jinghp on 2018/10/9.
//  Copyright © 2018年 sixue. All rights reserved.
//

import Cocoa

extension String.Encoding {
    static let gb18030_2000 = String.Encoding(rawValue: CFStringConvertEncodingToNSStringEncoding(CFStringEncoding(CFStringEncodings.GB_18030_2000.rawValue)))
}

class DocViewController: NSViewController {
    override func viewDidLoad() {
        super.viewDidLoad()
        
        tableView.delegate = self
        tableView.dataSource = self
        
        tableView.doubleAction = #selector(performDoubleClick)
        
        self.nextResponder = self.parent
    }
    
    override func viewWillAppear() {
        super.viewWillAppear()
        tableView.reloadData()
    }
    
    override func viewDidAppear() {
        super.viewDidAppear()
        self.view.becomeFirstResponder()
    }
    
    @objc func performDoubleClick() {
        forward()
    }
    
    func forward() {
        let rootPath = "/Users/jinghp/Dropbox/BBSReader.Cache"
        
        let row = tableView.selectedRow
        let item = Items[row]
        
        let path = rootPath + "/" + item.Source + "/" + item.ThreadId + ".txt"
        
        if let textData = (NSData.init(contentsOfFile: path ) as Data?) {
            if let str = String(data:textData, encoding:String.Encoding.utf8) {
                let mainVC = self.parent as! MainViewController
                mainVC.readText(str)
            }
        }
    }
    
    lazy var Items:[ListItem] = []
    @IBOutlet weak var tableView: NSTableView!
}

extension DocViewController: NSTableViewDataSource {
    func numberOfRows(in tableView: NSTableView) -> Int {
        return Items.count
    }
}

extension DocViewController: NSTableViewDelegate {
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

