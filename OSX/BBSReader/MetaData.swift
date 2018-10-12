//
//  MetaData.swift
//  BBSReader
//
//  Created by jinghp on 2018/10/9.
//  Copyright © 2018年 sixue. All rights reserved.
//

import Foundation

struct MetaData : Codable {
    var timestamp: Float
    let threads: [BBSThread]
    let tags: [String: [Int]]
    let superkeywords: [SuperKeyword]
    let blacklist: [String]
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
    var simple: Bool
    var keyword: String
    let author: [String]
    let tids: [Int]
    var read: Int
    let groups: [[Int]]
}

final class Meta {
    
    private init() {
        let jsonPath = Meta.ROOT_PATH + "/meta.json"
        let jsonData = NSData.init(contentsOfFile: jsonPath)
        let jsonDecoder = JSONDecoder()
        self.meta = try! jsonDecoder.decode(MetaData.self, from: jsonData! as Data)
    }
    
    static let ROOT_PATH = "/Users/jinghp/Dropbox/BBSReader.Cache"
    static let shared = Meta()
    
    var meta : MetaData
}
