import AppKit
import SwiftUI
import Foundation
import Darwin

private let appName = "Antigravity 中文助手"
private let appVersion = "0.6.7"
private let manifestURL = URL(string: "https://raw.githubusercontent.com/CX-ARTLab/antigravity-zh-assistant/main/translation/manifest.json")!

private enum AssistantError: LocalizedError {
    case noDebugPort
    case noTarget
    case invalidResponse

    var errorDescription: String? {
        switch self {
        case .noDebugPort: return "未找到 Antigravity 调试接口。请先启动 Antigravity。"
        case .noTarget: return "未找到可注入的 Antigravity 页面。"
        case .invalidResponse: return "Antigravity 调试接口返回了无效响应。"
        }
    }
}

private struct DevToolsTarget {
    let webSocketURL: URL
}

private final class CDPClient {
    private let socket: URLSessionWebSocketTask
    private var nextID = 1

    init(url: URL) {
        socket = URLSession.shared.webSocketTask(with: url)
        socket.resume()
    }

    func evaluate(_ expression: String) async throws -> Any? {
        let id = nextID
        nextID += 1
        let command: [String: Any] = [
            "id": id,
            "method": "Runtime.evaluate",
            "params": ["expression": expression, "returnByValue": true, "awaitPromise": false]
        ]
        let data = try JSONSerialization.data(withJSONObject: command)
        try await socket.send(.string(String(decoding: data, as: UTF8.self)))

        while true {
            let message = try await socket.receive()
            let payload: Data
            switch message {
            case .string(let text): payload = Data(text.utf8)
            case .data(let data): payload = data
            @unknown default: throw AssistantError.invalidResponse
            }
            guard let object = try JSONSerialization.jsonObject(with: payload) as? [String: Any] else { continue }
            guard let responseID = object["id"] as? Int, responseID == id else { continue }
            if let exception = object["exceptionDetails"] as? [String: Any] {
                throw NSError(domain: "CDP", code: 1, userInfo: [NSLocalizedDescriptionKey: String(describing: exception)])
            }
            let result = object["result"] as? [String: Any]
            return (result?["result"] as? [String: Any])?["value"]
        }
    }

    func close() {
        socket.cancel(with: .normalClosure, reason: nil)
    }
}

private final class DevToolsDiscovery {
    func targets() async throws -> [DevToolsTarget] {
        let ports = portCandidates().compactMap { url -> Int? in
            guard let text = try? String(contentsOf: url, encoding: .utf8),
                  let firstLine = text.split(whereSeparator: \.isNewline).first,
                  let port = Int(firstLine.trimmingCharacters(in: .whitespacesAndNewlines)) else { return nil }
            return port
        }
        guard !ports.isEmpty else { throw AssistantError.noDebugPort }
        for port in ports {
            guard let url = URL(string: "http://127.0.0.1:\(port)/json/list") else { continue }
            guard let (data, _) = try? await URLSession.shared.data(from: url),
                  let pages = try? JSONSerialization.jsonObject(with: data) as? [[String: Any]] else { continue }
            let matches = pages.compactMap { page -> DevToolsTarget? in
                guard (page["type"] as? String) == "page",
                      let rawURL = page["url"] as? String,
                      rawURL.contains("127.0.0.1") || rawURL.contains("localhost"),
                      let rawWebSocket = page["webSocketDebuggerUrl"] as? String,
                      let webSocketURL = URL(string: rawWebSocket) else { return nil }
                return DevToolsTarget(webSocketURL: webSocketURL)
            }
            if !matches.isEmpty { return matches }
        }
        throw AssistantError.noTarget
    }

    private func portCandidates() throws -> [URL] {
        let home = FileManager.default.homeDirectoryForCurrentUser
        let paths = [
            home.appendingPathComponent("Library/Application Support/Antigravity/DevToolsActivePort"),
            home.appendingPathComponent("Library/Application Support/Google/Antigravity/DevToolsActivePort"),
            home.appendingPathComponent(".antigravity/DevToolsActivePort")
        ]
        return paths.filter { FileManager.default.fileExists(atPath: $0.path) }
    }
}

@MainActor
private final class AppModel: ObservableObject {
    @Published var status = "尚未汉化"
    @Published var detail = ""
    @Published var isLocalized = false
    @Published var isBusy = false
    @Published var autoUpdate: Bool
    @Published var launchAtLogin: Bool
    @Published var antigravityVersion = "未检测"
    @Published var unknownCount = 0

    private var translationPack: [String: String] = [:]
    private var unknownStrings = Set<String>()
    private var monitorTask: Task<Void, Never>?
    private let discovery = DevToolsDiscovery()

    init() {
        autoUpdate = UserDefaults.standard.object(forKey: "AutoUpdate") as? Bool ?? true
        launchAtLogin = UserDefaults.standard.object(forKey: "LaunchAtLogin") as? Bool ?? false
        translationPack = Self.loadJSON(named: "translation-pack", ext: "json") ?? [:]
        monitorTask = Task { [weak self] in
            while !Task.isCancelled {
                try? await Task.sleep(nanoseconds: 3_000_000_000)
                guard let self, self.autoUpdate, !self.isBusy else { continue }
                await self.applyLocalization(silent: true)
            }
        }
    }

    deinit { monitorTask?.cancel() }

    func applyLocalization(silent: Bool = false) async {
        guard !isBusy else { return }
        isBusy = true
        status = "正在连接"
        detail = "正在连接 Antigravity 并应用汉化……"
        do {
            if autoUpdate { try await updateTranslationPack() }
            let targets = try await discovery.targets()
            let script = try makeTranslationScript()
            var found = Set<String>()
            for target in targets {
                let client = CDPClient(url: target.webSocketURL)
                _ = try await client.evaluate(script)
                if let values = try await client.evaluate("window.__antigravityZhAssistant?.collectUnknown?.() || []") as? [String] {
                    found.formUnion(values.map { $0.trimmingCharacters(in: .whitespacesAndNewlines) })
                }
                client.close()
            }
            unknownStrings = found
            unknownCount = found.count
            antigravityVersion = detectAntigravityVersion()
            saveReport()
            isLocalized = true
            status = "汉化已生效"
            detail = found.isEmpty ? "界面已扫描，当前没有发现待适配的系统词条。" : "发现 \(found.count) 条待适配系统文字。"
        } catch {
            if !silent { status = "暂未连接" }
            detail = error.localizedDescription
        }
        isBusy = false
    }

    func toggleLocalization() async {
        if isLocalized {
            do {
                let targets = try await discovery.targets()
                for target in targets {
                    let client = CDPClient(url: target.webSocketURL)
                    _ = try await client.evaluate("window.__antigravityZhAssistant?.restore?.() || {ok:true}")
                    client.close()
                }
                isLocalized = false
                status = "尚未汉化"
                detail = ""
            } catch { detail = error.localizedDescription }
        } else {
            await applyLocalization()
        }
    }

    func setAutoUpdate(_ value: Bool) {
        autoUpdate = value
        UserDefaults.standard.set(value, forKey: "AutoUpdate")
    }

    func setLaunchAtLogin(_ value: Bool) {
        launchAtLogin = value
        UserDefaults.standard.set(value, forKey: "LaunchAtLogin")
        LaunchAtLogin.setEnabled(value)
    }

    private func updateTranslationPack() async throws {
        let (manifestData, _) = try await URLSession.shared.data(from: manifestURL)
        guard let manifest = try JSONSerialization.jsonObject(with: manifestData) as? [String: Any],
              let version = manifest["version"] as? String,
              let packString = manifest["packUrl"] as? String,
              let url = URL(string: packString) else { return }
        if UserDefaults.standard.string(forKey: "TranslationPackVersion") == version { return }
        let (packData, _) = try await URLSession.shared.data(from: url)
        guard let pack = try JSONSerialization.jsonObject(with: packData) as? [String: String], !pack.isEmpty else { return }
        translationPack = pack
        try packData.write(to: Self.dataDirectory().appendingPathComponent("translation-pack.json"), options: .atomic)
        UserDefaults.standard.set(version, forKey: "TranslationPackVersion")
    }

    private func makeTranslationScript() throws -> String {
        guard let url = Self.resourceURL("translator", ext: "js") else { throw AssistantError.invalidResponse }
        var script = try String(contentsOf: url, encoding: .utf8)
        let json = try JSONSerialization.data(withJSONObject: translationPack)
        let encoded = String(decoding: json, as: UTF8.self)
        script = script.replacingOccurrences(of: "__AUTO_ADAPT__", with: autoUpdate ? "true" : "false")
        script = script.replacingOccurrences(of: "__EXTRA_TRANSLATIONS__", with: encoded)
        return script
    }

    private func saveReport() {
        let report: [String: Any] = [
            "assistantVersion": appVersion,
            "antigravityVersion": antigravityVersion,
            "scannedAt": ISO8601DateFormatter().string(from: Date()),
            "count": unknownStrings.count,
            "strings": unknownStrings.sorted()
        ]
        guard let data = try? JSONSerialization.data(withJSONObject: report, options: [.prettyPrinted]) else { return }
        try? data.write(to: Self.dataDirectory().appendingPathComponent("待适配词条.json"), options: .atomic)
    }

    private func detectAntigravityVersion() -> String {
        let home = FileManager.default.homeDirectoryForCurrentUser
        let candidates = [
            URL(fileURLWithPath: "/Applications/Antigravity.app"),
            URL(fileURLWithPath: "/Applications/Antigravity IDE.app"),
            home.appendingPathComponent("Applications/Antigravity.app"),
            home.appendingPathComponent("Applications/Antigravity IDE.app")
        ]
        for appURL in candidates {
            if let bundle = Bundle(url: appURL),
               let value = bundle.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String,
               !value.isEmpty { return value }
        }
        return "未安装"
    }

    private static func dataDirectory() -> URL {
        let url = FileManager.default.homeDirectoryForCurrentUser.appendingPathComponent("Library/Application Support/Antigravity 中文助手")
        try? FileManager.default.createDirectory(at: url, withIntermediateDirectories: true)
        return url
    }

    private static func resourceURL(_ name: String, ext: String) -> URL? {
        if let url = Bundle.main.url(forResource: name, withExtension: ext) { return url }
        let cwd = URL(fileURLWithPath: FileManager.default.currentDirectoryPath)
        let repositoryRoot = cwd.lastPathComponent == "macOS" ? cwd.deletingLastPathComponent() : cwd
        let candidates = [
            cwd.appendingPathComponent("Resources/\(name).\(ext)"),
            cwd.appendingPathComponent("macOS/Resources/\(name).\(ext)"),
            cwd.appendingPathComponent("src/\(name).\(ext)"),
            cwd.appendingPathComponent("translation/\(name).\(ext)"),
            repositoryRoot.appendingPathComponent("macOS/Resources/\(name).\(ext)"),
            repositoryRoot.appendingPathComponent("src/\(name).\(ext)"),
            repositoryRoot.appendingPathComponent("translation/\(name).\(ext)")
        ]
        return candidates.first { FileManager.default.fileExists(atPath: $0.path) }
    }

    private static func loadJSON<T: Decodable>(named name: String, ext: String) -> T? {
        guard let url = resourceURL(name, ext: ext), let data = try? Data(contentsOf: url) else { return nil }
        return try? JSONDecoder().decode(T.self, from: data)
    }
}

private enum LaunchAtLogin {
    static let label = "com.cxartlab.antigravity-zh-assistant"

    static func setEnabled(_ enabled: Bool) {
        let url = FileManager.default.homeDirectoryForCurrentUser.appendingPathComponent("Library/LaunchAgents/\(label).plist")
        if enabled {
            guard let executable = Bundle.main.executablePath else { return }
            let plist: [String: Any] = ["Label": label, "ProgramArguments": [executable], "RunAtLoad": true, "ProcessType": "Interactive"]
            try? FileManager.default.createDirectory(at: url.deletingLastPathComponent(), withIntermediateDirectories: true)
            (plist as NSDictionary).write(to: url, atomically: true)
            runLaunchctl(["bootstrap", "gui/\(getuid())", url.path])
        } else {
            runLaunchctl(["bootout", "gui/\(getuid())", url.path])
            try? FileManager.default.removeItem(at: url)
        }
    }

    private static func runLaunchctl(_ arguments: [String]) {
        let task = Process()
        task.executableURL = URL(fileURLWithPath: "/bin/launchctl")
        task.arguments = arguments
        try? task.run()
    }
}

private struct ContentView: View {
    @ObservedObject var model: AppModel

    var body: some View {
        ZStack {
            Color(red: 0.93, green: 0.94, blue: 0.96).ignoresSafeArea()
            VStack(spacing: 0) {
                Spacer().frame(height: 90)
                Image(systemName: "a.circle.fill")
                    .font(.system(size: 58, weight: .regular))
                    .foregroundStyle(.blue)
                Text("Welcome to Antigravity")
                    .font(.system(size: 30, weight: .regular, design: .rounded))
                    .foregroundColor(Color(red: 0.30, green: 0.31, blue: 0.42))
                    .padding(.top, 26)
                VStack(spacing: 0) {
                    Text("汉化助手 v\(appVersion)")
                        .font(.system(size: 18, weight: .bold))
                        .foregroundColor(Color(red: 0.30, green: 0.31, blue: 0.42))
                        .padding(.top, 20)
                    Button { Task { await model.toggleLocalization() } } label: {
                        Text(model.isLocalized ? "✓  汉化已生效" : model.status)
                            .font(.system(size: 18, weight: .bold))
                            .frame(maxWidth: .infinity, minHeight: 42)
                    }
                    .buttonStyle(.borderedProminent)
                    .tint(Color(red: 0.52, green: 0.18, blue: 0.93))
                    .padding(.horizontal, 28)
                    .padding(.top, 18)
                    HStack(spacing: 24) {
                        Toggle("自动更新", isOn: Binding(get: { model.autoUpdate }, set: model.setAutoUpdate))
                        Toggle("开机启动", isOn: Binding(get: { model.launchAtLogin }, set: model.setLaunchAtLogin))
                    }
                    .toggleStyle(.checkbox)
                    .font(.system(size: 14))
                    .padding(.top, 18)
                }
                .frame(width: 344, height: 168)
                .background(Color(red: 0.925, green: 0.937, blue: 0.949))
                .clipShape(RoundedRectangle(cornerRadius: 12))
                .overlay(RoundedRectangle(cornerRadius: 12).stroke(Color.gray.opacity(0.24)))
                .padding(.top, 48)
                Text("Antigravity 未检测  ·  待适配 \(model.unknownCount)")
                    .font(.system(size: 14))
                    .foregroundColor(Color(red: 0.40, green: 0.42, blue: 0.52))
                    .padding(.top, 16)
                if !model.detail.isEmpty {
                    Text(model.detail).font(.system(size: 11)).foregroundColor(.secondary).padding(.top, 8)
                }
                Spacer()
            }
        }
        .frame(width: 500, height: 550)
    }
}

@main
struct AntigravityZhAssistantMacApp: App {
    @StateObject private var model = AppModel()

    var body: some Scene {
        WindowGroup { ContentView(model: model) }
        .windowStyle(.hiddenTitleBar)
    }
}
