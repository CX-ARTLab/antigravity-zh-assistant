// swift-tools-version: 5.7
import PackageDescription

let package = Package(
    name: "AntigravityZhAssistantMac",
    platforms: [.macOS(.v12)],
    products: [
        .executable(name: "AntigravityZhAssistantMac", targets: ["AntigravityZhAssistantMac"])
    ],
    targets: [
        .executableTarget(name: "AntigravityZhAssistantMac")
    ]
)
