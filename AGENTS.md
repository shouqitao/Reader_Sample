# Repository Guidelines

## Project Structure & Modules
- `Reader_Sample/`: WinForms app (.NET Framework 4.8). Forms include `Form1`, `m1`, `CPU`, `15693`, `sfz`; outputs under `bin/` per configuration.
- `CardReaderLib/`: Reusable library (folders: `Core/`, `Models/`, `Exceptions/`, `Examples/`). Primary entry: `CardReaderManager.cs`.
- `CardReaderWPF/`: WPF demo UI (e.g., `MainWindow.xaml`, `Services/`).
- Root: `CardReader.sln`, `build.bat`, and solution/project files; build artifacts in `bin/` and `obj/`.

## Build, Run, and Dev Commands
- Build all (x86 Debug): `build.bat` — discovers MSBuild and builds `CardReaderLib`, `CardReaderWPF`, then `CardReader.sln`.
- Build solution: `msbuild CardReader.sln /p:Configuration=Debug /p:Platform=x86 /v:m`.
- Build single project: `msbuild CardReaderLib\CardReaderLib.csproj /p:Configuration=Debug /p:Platform=x86`.
- Run WinForms sample: `Reader_Sample\bin\Debug\Reader_Sample.exe`.
- Run WPF sample: `CardReaderWPF\bin\Debug\CardReaderWPF.exe`.
- Clean: `msbuild CardReader.sln /t:Clean`.

## Coding Style & Naming
- Language: C# 7.3; Target: .NET Framework 4.8; indent 4 spaces; UTF-8.
- Naming: PascalCase for types/methods/properties; camelCase for locals/parameters; private fields `_camelCase` (e.g., `_readerHandle`).
- Structure: one type per file; namespaces reflect folders (e.g., `CardReaderLib.Models`). Prefer explicit access modifiers and early returns.
- Docs: XML doc comments for public APIs; add concise Chinese comments where it clarifies device behavior.

## Testing Guidelines
- No test project is checked in. For library code, add `CardReaderLib.Tests` (MSTest or NUnit). Name test classes `*Tests` and methods `MethodName_Should...`.
- Run (VS): Test Explorer. CLI example: `vstest.console.exe CardReaderLib.Tests\bin\Debug\CardReaderLib.Tests.dll`.
- Focus: cover `CardReaderManager` flows, error codes, and native interop boundaries.

## Commit & Pull Requests
- Commits: one logical change; imperative mood; concise English/Chinese. Example: `Fix HD reader init on x86`.
- Reference issues (`#123`), include rationale if touching hardware timing or error codes.
- PRs: clear description, steps to validate (device/driver versions), linked issues; screenshots for UI changes; note any config keys added to `app.config`.

## Security & Configuration
- Do not commit device IDs, licenses, or personal photos. Keep secrets out of `app.config`; provide sample values.
- Document required drivers and versions in PRs that change device initialization or transport (USB/COM).

