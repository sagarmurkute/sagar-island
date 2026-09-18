# Contributing to Sagar Island

Thank you for your interest in contributing to **Sagar Island**! We welcome contributions of all kinds: bug fixes, new features, UI refinements, translations, and documentation improvements.

---

## 🛠️ Development Setup

### Requirements
1. **Windows 10 (Build 19041+)** or **Windows 11**
2. **[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)**
3. **Visual Studio 2022** (with .NET desktop development workload) or **VS Code** with C# Dev Kit.

### Clone & Run
```bash
# Clone the repository
git clone https://github.com/sagarmurkute/sagar-island.git
cd sagar-island

# Build and run locally
dotnet run
```

---

## 🌿 Contribution Workflow

1. **Fork the repo** and create your branch from `main`:
   ```bash
   git checkout -b feature/my-amazing-feature
   ```
2. **Make your changes**:
   - Adhere to the `Segoe UI Variable` typography hierarchy.
   - Ensure all layout margins/paddings snap to the **4px base spatial grid**.
   - Keep animations fluid and matching the spring physics easing curves.
3. **Test your build**:
   ```bash
   dotnet build
   ```
4. **Commit with descriptive messages** following Conventional Commits (`feat:`, `fix:`, `docs:`, `style:`, `refactor:`).
5. **Push and open a Pull Request** against the `main` branch.

---

## 📜 Code of Conduct
This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md). Please be respectful and welcoming to all contributors.
