import AppKit
import SwiftUI
import Foundation
import Darwin

private let appName = "Antigravity 中文助手"
private let appVersion = (Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String) ?? "0.6.11"
private let manifestURL = URL(string: "https://raw.githubusercontent.com/CX-ARTLab/antigravity-zh-assistant/main/translation/manifest.json")!

private func assistantIconImage() -> NSImage {
    if let bundled = Bundle.main.url(forResource: "assistant-icon", withExtension: "png"),
       let image = NSImage(contentsOf: bundled) { return image }
    let cwd = URL(fileURLWithPath: FileManager.default.currentDirectoryPath)
    let repositoryRoot = cwd.lastPathComponent == "macOS" ? cwd.deletingLastPathComponent() : cwd
    let candidates = [
        cwd.appendingPathComponent("Resources/assistant-icon.png"),
        repositoryRoot.appendingPathComponent("macOS/Resources/assistant-icon.png"),
        repositoryRoot.appendingPathComponent("src/Assets/assistant-icon.png")
    ]
    for url in candidates {
        if let image = NSImage(contentsOf: url) { return image }
    }
    return NSImage(size: NSSize(width: 64, height: 64))
}

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
            let result = object["result"] as? [String: Any]
            if let exception = result?["exceptionDetails"] as? [String: Any] {
                throw NSError(domain: "CDP", code: 1, userInfo: [NSLocalizedDescriptionKey: String(describing: exception)])
            }
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

    private func portCandidates() -> [URL] {
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
        launchAtLogin = LaunchAtLogin.isEnabled
        translationPack = Self.loadTranslationPack()
        antigravityVersion = detectAntigravityVersion()
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
            var updateFailed = false
            if autoUpdate {
                do { try await updateTranslationPack() }
                catch { updateFailed = true }
            }
            let targets = try await discovery.targets()
            let script = try makeTranslationScript()
            var found = Set<String>()
            for target in targets {
                let client = CDPClient(url: target.webSocketURL)
                _ = try await client.evaluate("localStorage.removeItem('__antigravityZhAssistantDisabled'); true")
                guard let outcome = try await client.evaluate(script) as? [String: Any],
                      outcome["ok"] as? Bool == true,
                      outcome["disabled"] == nil else {
                    client.close()
                    throw AssistantError.invalidResponse
                }
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
            let scanDetail = found.isEmpty ? "界面已扫描，当前没有发现待适配的系统词条。" : "发现 \(found.count) 条待适配系统文字。"
            detail = updateFailed ? "\(scanDetail) 自动更新暂不可用，已使用本地词典。" : scanDetail
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
        let bundledVersion = Self.bundledTranslationPackVersion()
        guard Self.comparePackVersions(version, bundledVersion) == .orderedDescending else { return }
        if UserDefaults.standard.string(forKey: "TranslationPackVersion") == version { return }
        let (packData, _) = try await URLSession.shared.data(from: url)
        guard let pack = try JSONSerialization.jsonObject(with: packData) as? [String: String], !pack.isEmpty else { return }
        translationPack = Self.addingMacOSOverrides(to: pack)
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

    private static func loadTranslationPack() -> [String: String] {
        let bundled = addingMacOSOverrides(to: loadJSON(named: "translation-pack", ext: "json") ?? [:])
        let bundledVersion = bundledTranslationPackVersion()
        let cachedVersion = UserDefaults.standard.string(forKey: "TranslationPackVersion") ?? ""
        let cachedURL = dataDirectory().appendingPathComponent("translation-pack.json")
        if comparePackVersions(cachedVersion, bundledVersion) == .orderedDescending,
           let data = try? Data(contentsOf: cachedURL),
           let cached = try? JSONDecoder().decode([String: String].self, from: data),
           !cached.isEmpty { return addingMacOSOverrides(to: cached) }
        return bundled
    }

    private static func bundledTranslationPackVersion() -> String {
        let manifest: [String: String] = loadJSON(named: "translation-manifest", ext: "json") ?? [:]
        return manifest["version"] ?? ""
    }

    private static func comparePackVersions(_ left: String, _ right: String) -> ComparisonResult {
        left.compare(right, options: [.numeric, .caseInsensitive])
    }

    private static func addingMacOSOverrides(to pack: [String: String]) -> [String: String] {
        var result = pack
        let overrides = [
            "Configures how the agent tries to access files outside of its working folders.": "配置智能体尝试访问工作文件夹以外文件的方式。",
            "Controls whether terminal commands require your approval before running.": "控制终端命令在运行前是否需要你的批准。",
            "Enable Sandbox Mode (Preview)": "启用沙盒模式（预览）",
            "Guidelines for interacting with GitHub and request permissions from the user when commands fail due to restrictions in the agent environment.": "用于与 GitHub 交互的指南；当命令因智能体环境限制而失败时，请向用户请求权限。",
            "Install IDE": "安装 IDE",
            "No MCP servers installed": "未安装 MCP 服务器",
            "Outside of folders file access policy": "工作文件夹外的文件访问策略",
            "Proceed in Sandbox": "在沙盒中继续",
            "Restricts agent tools to a secure, isolated local sandbox.": "将智能体工具限制在安全、隔离的本地沙盒中。",
            "Terminal Command Auto Execution": "终端命令自动执行",
            "There was an unexpected issue setting up your account.": "设置账号时出现意外问题。",
            "Continue with different account": "使用其他账号继续",
            "Having trouble? Let us know": "遇到问题？请告诉我们",
            "There are no customizations enabled.": "当前未启用任何自定义项。",
            "Use Add MCP to browse the store, or add a custom server via the MCP config.": "使用“添加 MCP”浏览商店，或通过 MCP 配置添加自定义服务器。"
        ]
        for (english, chinese) in overrides { result[english] = chinese }
        return result
    }
}

private enum LaunchAtLogin {
    static let label = "com.cxartlab.antigravity-zh-assistant"

    static var isEnabled: Bool {
        FileManager.default.fileExists(atPath: agentURL.path)
    }

    private static var agentURL: URL {
        FileManager.default.homeDirectoryForCurrentUser.appendingPathComponent("Library/LaunchAgents/\(label).plist")
    }

    static func setEnabled(_ enabled: Bool) {
        let url = agentURL
        if enabled {
            guard let executable = Bundle.main.executablePath else { return }
            let plist: [String: Any] = ["Label": label, "ProgramArguments": [executable], "RunAtLoad": true, "ProcessType": "Interactive"]
            try? FileManager.default.createDirectory(at: url.deletingLastPathComponent(), withIntermediateDirectories: true)
            (plist as NSDictionary).write(to: url, atomically: true)
            runLaunchctl(["bootstrap", "gui/\(getuid())", url.path])
        } else {
            runLaunchctl(["bootout", "gui/\(getuid())/\(label)"])
            try? FileManager.default.removeItem(at: url)
        }
    }

    private static func runLaunchctl(_ arguments: [String]) {
        let task = Process()
        task.executableURL = URL(fileURLWithPath: "/bin/launchctl")
        task.arguments = arguments
        do {
            try task.run()
            task.waitUntilExit()
        } catch { }
    }
}

private struct WindowConfigurator: NSViewRepresentable {
    func makeNSView(context: Context) -> NSView {
        let view = NSView()
        DispatchQueue.main.async { configure(view.window) }
        return view
    }

    func updateNSView(_ view: NSView, context: Context) {
        DispatchQueue.main.async { configure(view.window) }
    }

    private func configure(_ window: NSWindow?) {
        guard let window else { return }
        let size = NSSize(width: 500, height: 550)
        guard window.contentView?.frame.size != size || window.styleMask.contains(.resizable) else { return }
        window.setContentSize(size)
        window.minSize = size
        window.maxSize = size
        window.styleMask.remove(.resizable)
        window.center()
    }
}

private struct ContentView: View {
    @ObservedObject var model: AppModel

    var body: some View {
        ZStack {
            Color(red: 0.93, green: 0.94, blue: 0.96).ignoresSafeArea()
            VStack(spacing: 0) {
                Spacer().frame(height: 90)
                ZStack {
                    Circle()
                        .fill(RadialGradient(colors: [Color.cyan.opacity(0.25), .clear], center: .center, startRadius: 0, endRadius: 72))
                        .frame(width: 148, height: 158)
                        .offset(x: -32, y: -8)
                    Circle()
                        .fill(RadialGradient(colors: [Color.yellow.opacity(0.20), .clear], center: .center, startRadius: 0, endRadius: 72))
                        .frame(width: 148, height: 148)
                        .offset(x: 24, y: -22)
                    Circle()
                        .fill(RadialGradient(colors: [Color.red.opacity(0.17), .clear], center: .center, startRadius: 0, endRadius: 68))
                        .frame(width: 134, height: 144)
                        .offset(x: 54, y: -4)
                    Circle()
                        .fill(RadialGradient(colors: [Color.indigo.opacity(0.18), .clear], center: .center, startRadius: 0, endRadius: 74))
                        .frame(width: 151, height: 150)
                        .offset(x: 9, y: 37)
                    Image(nsImage: assistantIconImage())
                        .resizable()
                        .interpolation(.high)
                        .frame(width: 64, height: 64)
                }
                .frame(width: 64, height: 64)
                Text("欢迎使用 Antigravity")
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
                    .foregroundColor(Color(red: 0.30, green: 0.31, blue: 0.42))
                    .padding(.top, 18)
                }
                .frame(width: 344, height: 168)
                .background(Color(red: 0.925, green: 0.937, blue: 0.949))
                .clipShape(RoundedRectangle(cornerRadius: 12))
                .overlay(RoundedRectangle(cornerRadius: 12).stroke(Color.gray.opacity(0.24)))
                .padding(.top, 48)
                Text("Antigravity \(model.antigravityVersion)  ·  待适配 \(model.unknownCount)")
                    .font(.system(size: 14))
                    .foregroundColor(Color(red: 0.40, green: 0.42, blue: 0.52))
                    .padding(.top, 16)
                if !model.detail.isEmpty {
                    Text(model.detail)
                        .font(.system(size: 11))
                        .foregroundColor(Color(red: 0.40, green: 0.42, blue: 0.52))
                        .padding(.top, 8)
                }
                Spacer()
            }
        }
        .frame(width: 500, height: 550)
        .background(WindowConfigurator())
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
