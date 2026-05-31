//
//  AzJSONDecoder.swift
//  AzPin
//
//  Created by Lucas Mundim on 31/05/2026.
//

import Foundation

final class AzJSONDecoder: Sendable {
    private let decoder: JSONDecoder
    
    init() {
        self.decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .secondsSince1970
    }
    
    /// Decode JSON string to a specific type
    func decode<T: Decodable>(_ type: T.Type, from json: String) throws -> T {
        return try decoder.decode(type, from: Data(json.utf8))
    }
    
    /// Decode JSON data to a specific type
    func decode<T: Decodable>(_ type: T.Type, from data: Data) throws -> T {
        return try decoder.decode(type, from: data)
    }
}
