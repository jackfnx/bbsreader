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
        
        textView.font = NSFont.userFont(ofSize: 18)
        
        self.nextResponder = self.parent
    }
    
    override func viewWillAppear() {
        textView.string = Text
        scrollView.contentView.scroll(to: NSPoint(x: 0, y: 0))
    }
    
    override func viewDidAppear() {
        super.viewDidAppear()
        self.view.becomeFirstResponder()
    }
    
    lazy var Text: String = ""
    
    @IBOutlet var scrollView: NSScrollView!
    @IBOutlet var textView: NSTextView!
}
