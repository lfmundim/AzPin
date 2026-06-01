//
//  AzJSONDecoderTests.swift
//  AzPin
//
//  Created by Lucas Mundim on 31/05/2026.
//
import XCTest
@testable import AzPin

final class AzJSONDecoderTests: XCTestCase {
    func test_decodeExpiresOn_fromUnixTimestamp() throws {
        // expires_on is a UTC Unix timestamp — immune to local timezone offset
        let json = """
        {
            "accessToken": "tok",
            "expires_on": 1748784600
        }
        """

        let decoder = AzJSONDecoder()
        let response = try decoder.decode(AzureTokenResponse.self, from: Data(json.utf8))

        XCTAssertEqual(response.accessToken, "tok")
        XCTAssertEqual(response.expiresOn, Date(timeIntervalSince1970: 1748784600))
    }
}
