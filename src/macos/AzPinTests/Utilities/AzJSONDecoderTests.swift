//
//  AzJSONDecoderTests.swift
//  AzPin
//
//  Created by Lucas Mundim on 31/05/2026.
//
import XCTest
@testable import AzPin

final class AzJSONDecoderTests: XCTestCase {
    func test_decodeExpiresOn_fromAzCLIFormat() throws {
        // Arrange
        let json = """
        {
            "accessToken": "tok",
            "expiresOn": "2025-06-01 14:30:00.000000"
        }
        """
        
        let decoder = AzJSONDecoder()
        
        // Act
        let response = try decoder.decode(AzureTokenResponse.self, from: Data(json.utf8))
        
        // Assert
        XCTAssertEqual(response.accessToken, "tok")
        XCTAssertNotNil(response.expiresOn)
        let calendar = Calendar.current
        let components = calendar.dateComponents([.day, .month, .year], from: response.expiresOn)
        XCTAssertEqual(components.day, 1)
        XCTAssertEqual(components.month, 6)
        XCTAssertEqual(components.year, 2025)
    }
}
