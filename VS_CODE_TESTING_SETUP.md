# Recommended VS Code Extensions for Testing & Coverage

## Essential Testing Extensions:

1. **Jest** (Orta.vscode-jest)
   - Real-time test running and results
   - Inline test status indicators
   - Test debugging support

2. **Jest Runner** (firsttris.vscode-jest-runner)
   - Run individual tests with CodeLens
   - Right-click context menu for tests

3. **Coverage Gutters** (ryanluker.vscode-coverage-gutters)
   - Shows coverage inline in editor
   - Highlights covered/uncovered lines
   - Works with lcov files

4. **Test Explorer UI** (hbenl.vscode-test-explorer)
   - Unified test interface
   - Test tree view in sidebar

## C# Testing Extensions:

5. **.NET Core Test Explorer** (formulahendry.dotnet-test-explorer)
   - Run C# tests from VS Code
   - xUnit, NUnit, MSTest support

6. **C# Dev Kit** (ms-dotnettools.csdevkit)
   - Includes test running capabilities
   - Solution explorer with test integration

## Installation Commands:
```bash
# Install via VS Code extensions marketplace or command palette
# Search for: "Jest", "Coverage Gutters", "Test Explorer"
```

## Coverage Visualization Setup:
1. Install "Coverage Gutters" extension
2. Run tests with coverage: `npm run test:coverage`
3. Open Command Palette (Ctrl+Shift+P)
4. Run: "Coverage Gutters: Display Coverage"
5. Coverage will show as colored gutters in editor:
   - 🟢 Green: Covered lines
   - 🔴 Red: Uncovered lines
   - 🟡 Yellow: Partially covered (branches)

## Real-time Testing:
- Jest extension will show test status in real-time
- Green ✅ / Red ❌ indicators next to test functions
- Failing tests show inline error messages
- Run tests automatically on file save