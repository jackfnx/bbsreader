//
//  ReadViewController.swift
//  BBSReader
//
//  Created by jinghp on 2018/10/9.
//  Copyright © 2018年 sixue. All rights reserved.
//

import Cocoa

class ConViewController: NSViewController {

    override func viewDidLoad() {
        super.viewDidLoad()
        
        self.textView.font = NSFont.userFont(ofSize: 18)
        
        self.nextResponder = self.parent
    }
    
    override func viewWillAppear() {
        self.textView.string = self.text
        self.scrollView.contentView.scroll(to: NSPoint(x: 0, y: 0))
    }
    
    override func viewDidAppear() {
        super.viewDidAppear()
        self.view.becomeFirstResponder()
    }
    
    private lazy var text: String = ""
    func importData(_ text: String) {
        self.text = text
    }
    
    @IBOutlet var scrollView: NSScrollView!
    @IBOutlet var textView: NSTextView!
}
