import Foundation

enum UpdateCheckState: Equatable, Sendable {
    case idle
    case checking
    case upToDate(version: String)
    case updateAvailable(current: String, latest: String, releaseURL: URL)
    case failed(String)
}

@MainActor
protocol UpdateCheckServiceProtocol: AnyObject {
    var state: UpdateCheckState { get }
    func checkForUpdates() async
}

@MainActor
@Observable
final class UpdateCheckService: UpdateCheckServiceProtocol {
    private(set) var state: UpdateCheckState = .idle
    private let session: URLSession

    private static let apiURL = URL(string: "https://api.github.com/repos/lfmundim/AzPin/releases/latest")!

    init(session: URLSession = .shared) {
        self.session = session
    }

    func checkForUpdates() async {
        guard state != .checking else { return }
        state = .checking
        do {
            var request = URLRequest(url: Self.apiURL)
            request.setValue("application/vnd.github+json", forHTTPHeaderField: "Accept")
            request.setValue("AzPin", forHTTPHeaderField: "User-Agent")
            let (data, _) = try await session.data(for: request)
            let release = try JSONDecoder().decode(GitHubRelease.self, from: data)
            let latest = release.tagName.drop(while: { $0 == "v" || $0 == "V" })
            let current = Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "0.0.0"
            if Self.isNewer(String(latest), than: current) {
                state = .updateAvailable(current: current, latest: String(latest), releaseURL: release.htmlURL)
            } else {
                state = .upToDate(version: current)
            }
        } catch {
            state = .failed(error.localizedDescription)
        }
    }

    static func isNewer(_ version: String, than current: String) -> Bool {
        let parse: (String) -> [Int] = { $0.split(separator: ".").compactMap { Int($0) } }
        let l = parse(version), c = parse(current)
        for i in 0..<max(l.count, c.count) {
            let lv = i < l.count ? l[i] : 0
            let cv = i < c.count ? c[i] : 0
            if lv != cv { return lv > cv }
        }
        return false
    }
}

private struct GitHubRelease: Decodable {
    let tagName: String
    let htmlURL: URL

    enum CodingKeys: String, CodingKey {
        case tagName = "tag_name"
        case htmlURL = "html_url"
    }
}
